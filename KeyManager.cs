using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeiShuiCS2
{
    public class KeyManager
    {
        private static KeyManager? _instance;
        private BeiShuiCipher? _cipher;
        private int sequenceNumber = 0;
        private DateTime keyExpireTime = DateTime.MinValue;
        private string? rsaPrivateKeyXml;
        private string? serverIP;
        private int heartbeatPort;

        private KeyManager() { }

        public static KeyManager Instance => _instance ??= new KeyManager();

        public static async Task<bool> Init(string serverIP, int heartbeatPort, string steamId)
        {
            return await Instance.InternalInit(serverIP, heartbeatPort, steamId);
        }

        private async Task<bool> InternalInit(string serverIP, int heartbeatPort, string steamId)
        {
            try
            {
                this.serverIP = serverIP;
                this.heartbeatPort = heartbeatPort;

                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    string publicKeyXml = rsa.ToXmlString(false);
                    string privateKeyXml = rsa.ToXmlString(true);
                    rsaPrivateKeyXml = privateKeyXml;

                    var request = new HandshakeRequest { SteamID = steamId, PublicKey = publicKeyXml };
                    string json = JsonSerializer.Serialize(request);
                    byte[] requestData = Encoding.UTF8.GetBytes(json);

                    byte[] packet = new byte[2 + requestData.Length];
                    packet[0] = 0xBE;
                    packet[1] = 0x5E;
                    Buffer.BlockCopy(requestData, 0, packet, 2, requestData.Length);

                    using var udpClient = new UdpClient();
                    udpClient.Client.ReceiveTimeout = 5000;
                    await udpClient.SendAsync(packet, packet.Length, serverIP, heartbeatPort);

                    var result = await udpClient.ReceiveAsync();
                    byte[] response = result.Buffer;

                    if (response.Length < 4 || response[0] != 0xBE || response[1] != 0x5F)
                    {
                        return false;
                    }

                    int seedLen = (response[2] << 8) | response[3];
                    if (response.Length < 4 + seedLen) return false;

                    byte[] encryptedSeed = new byte[seedLen];
                    Buffer.BlockCopy(response, 4, encryptedSeed, 0, seedLen);

                    byte[] seed;
                    using (var rsaDecrypt = new RSACryptoServiceProvider())
                    {
                        rsaDecrypt.FromXmlString(rsaPrivateKeyXml);
                        seed = rsaDecrypt.Decrypt(encryptedSeed, false);
                    }

                    if (seed.Length != 32) return false;

                    _cipher = new BeiShuiCipher();
                    _cipher.Init(seed);
                    keyExpireTime = DateTime.UtcNow.AddHours(1);
                    sequenceNumber = 0;
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KeyManager] 握手失败: {ex.Message}");
                return false;
            }
        }

        public bool IsKeyValid() => _cipher != null && DateTime.UtcNow < keyExpireTime;

        public BeiShuiCipher GetCipher() => _cipher!;

        public int GetNextSequence() => sequenceNumber++;

        private class HandshakeRequest
        {
            public string SteamID { get; set; } = "";
            public string PublicKey { get; set; } = "";
        }
    }
}