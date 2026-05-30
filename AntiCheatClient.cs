using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BeiShuiCS2
{
    /// <summary>
    /// 反作弊客户端通讯模块 — 与服务器 BAC 插件进行 UDP 握手 + 加密心跳
    /// 防止用户伪造心跳或重放攻击
    /// </summary>
    public class AntiCheatClient : IDisposable
    {
        // ===== 常量 =====
        private const byte MAGIC_HANDSHAKE = 0xBE;
        private const byte CMD_HANDSHAKE = 0x5E;
        private const byte CMD_HEARTBEAT = 0x5F;
        private const byte CMD_CHALLENGE_RESP = 0x60;
        private const int HEARTBEAT_INTERVAL_MS = 8000;
        private const int HANDSHAKE_TIMEOUT_MS = 5000;
        private const int MAX_RETRIES = 3;

        // ===== 状态 =====
        private UdpClient? _udpClient;
        private IPEndPoint? _serverEndpoint;
        private BeiShuiCipher? _cipher;
        private CancellationTokenSource? _cts;
        private Task? _heartbeatTask;
        private Task? _receiveTask;
        private readonly object _lock = new();

        private int _sequence;
        private bool _handshakeComplete;
        private bool _isDisposed;

        // RSA 密钥对（每个会话生成一次）
        private RSACryptoServiceProvider? _rsa;
        private string? _publicKeyXml;
        private ulong _localSteamId;

        // 服务器挑战相关
        private byte[]? _lastChallenge;
        private DateTime _lastChallengeTime;

        /// <summary>当前是否已与服务器建立安全通道</summary>
        public bool IsConnected => _handshakeComplete && _cipher != null;

        /// <summary>是否已释放</summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>最后成功心跳时间</summary>
        public DateTime LastHeartbeatTime { get; private set; } = DateTime.MinValue;

        /// <summary>丢失心跳计数</summary>
        public int MissedHeartbeats { get; private set; }

        /// <summary>连接状态变更事件</summary>
        public event Action<bool>? OnConnectionChanged;

        public AntiCheatClient()
        {
            _localSteamId = 0;
            try
            {
                // 如果用户已通过 Steam 登录，获取 SteamID
                if (App.CurrentUser != null && !string.IsNullOrEmpty(App.CurrentUser.SteamId))
                {
                    ulong.TryParse(App.CurrentUser.SteamId, out _localSteamId);
                }
            }
            catch { }

            GenerateRSAKeyPair();
        }

        /// <summary>
        /// 设置目标服务器并启动心跳连接
        /// </summary>
        public void ConnectToServer(string ip, int gamePort)
        {
            if (_isDisposed) return;

            Disconnect();

            lock (_lock)
            {
                _serverEndpoint = new IPEndPoint(IPAddress.Parse(ip), gamePort + 1);
                _handshakeComplete = false;
                _cipher = null;
                _sequence = 0;

                try
                {
                    _udpClient = new UdpClient();
                    _udpClient.Client.SendTimeout = 3000;
                    _udpClient.Client.ReceiveTimeout = 3000;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BAC-Client] UDP 创建失败: {ex.Message}");
                    return;
                }

                _cts = new CancellationTokenSource();
                _heartbeatTask = Task.Run(() => HeartbeatLoop(_cts.Token));
                _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));
            }
        }

        /// <summary>
        /// 断开与服务器的连接
        /// </summary>
        public void Disconnect()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _heartbeatTask = null;
                _receiveTask = null;
                _handshakeComplete = false;
                _cipher = null;
                _serverEndpoint = null;

                try { _udpClient?.Close(); } catch { }
                _udpClient = null;
            }

            OnConnectionChanged?.Invoke(false);
        }

        /// <summary>
        /// 生成 RSA 密钥对用于握手阶段的种子交换
        /// </summary>
        private void GenerateRSAKeyPair()
        {
            try
            {
                _rsa = new RSACryptoServiceProvider(2048);
                _publicKeyXml = _rsa.ToXmlString(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BAC-Client] RSA 生成失败: {ex.Message}");
                _rsa = null;
            }
        }

        /// <summary>
        /// 心跳主循环：握手 → 定时发送加密心跳
        /// </summary>
        private async Task HeartbeatLoop(CancellationToken ct)
        {
            int retryCount = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!_handshakeComplete)
                    {
                        // 尝试握手
                        bool handshakeOk = await PerformHandshakeAsync(ct);
                        if (handshakeOk)
                        {
                            _handshakeComplete = true;
                            retryCount = 0;
                            OnConnectionChanged?.Invoke(true);
                            System.Diagnostics.Debug.WriteLine("[BAC-Client] 握手成功，安全通道已建立");
                        }
                        else
                        {
                            retryCount++;
                            if (retryCount >= MAX_RETRIES)
                            {
                                // 连续失败，等待重试
                                System.Diagnostics.Debug.WriteLine($"[BAC-Client] 握手失败({retryCount}/{MAX_RETRIES})");
                            }
                            await Task.Delay(3000, ct);
                            continue;
                        }
                    }

                    // 发送加密心跳
                    if (_handshakeComplete && _cipher != null)
                    {
                        bool sent = await SendEncryptedHeartbeatAsync(ct);
                        if (sent)
                        {
                            MissedHeartbeats = 0;
                            LastHeartbeatTime = DateTime.UtcNow;
                        }
                        else
                        {
                            MissedHeartbeats++;
                            System.Diagnostics.Debug.WriteLine($"[BAC-Client] 心跳发送失败 ({MissedHeartbeats})");

                            if (MissedHeartbeats >= 3)
                            {
                                // 连接丢失，重新握手
                                System.Diagnostics.Debug.WriteLine("[BAC-Client] 连接丢失，重新握手");
                                _handshakeComplete = false;
                                _cipher = null;
                                OnConnectionChanged?.Invoke(false);
                            }
                        }
                    }

                    await Task.Delay(HEARTBEAT_INTERVAL_MS, ct);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BAC-Client] 心跳循环异常: {ex.Message}");
                    await Task.Delay(5000, ct);
                }
            }
        }

        /// <summary>
        /// UDP 接收循环：监听服务器发来的挑战包 (0xBE 0x60)
        /// </summary>
        private async Task ReceiveLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    UdpClient? udp;
                    lock (_lock) { udp = _udpClient; }

                    if (udp == null) break;

                    var result = await udp.ReceiveAsync();
                    byte[] data = result.Buffer;

                    if (data.Length < 2) continue;

                    // 服务器挑战包: 0xBE 0x60 + 挑战(32字节)
                    if (data[0] == MAGIC_HANDSHAKE && data[1] == CMD_CHALLENGE_RESP && data.Length >= 34)
                    {
                        byte[] challenge = new byte[32];
                        Buffer.BlockCopy(data, 2, challenge, 0, 32);
                        SetChallenge(challenge);
                        System.Diagnostics.Debug.WriteLine("[BAC-Client] 收到服务器挑战");
                    }
                }
                catch (ObjectDisposedException) { break; }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BAC-Client] 接收异常: {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }
        }

        /// <summary>
        /// UDP 握手：发送公钥 → 接收加密种子 → 初始化加密器
        /// </summary>
        private async Task<bool> PerformHandshakeAsync(CancellationToken ct)
        {
            if (_rsa == null || string.IsNullOrEmpty(_publicKeyXml) || _serverEndpoint == null)
                return false;

            UdpClient? udp;
            IPEndPoint? endpoint;

            lock (_lock)
            {
                udp = _udpClient;
                endpoint = _serverEndpoint;
            }

            if (udp == null || endpoint == null) return false;

            try
            {
                // 构建握手请求: MAGIC + CMD + JSON(SteamID, PublicKey)
                var handshakeReq = new
                {
                    SteamID = _localSteamId.ToString(),
                    PublicKey = _publicKeyXml
                };
                string json = JsonSerializer.Serialize(handshakeReq);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                byte[] packet = new byte[2 + jsonBytes.Length];
                packet[0] = MAGIC_HANDSHAKE;
                packet[1] = CMD_HANDSHAKE;
                Buffer.BlockCopy(jsonBytes, 0, packet, 2, jsonBytes.Length);

                await udp.SendAsync(packet, packet.Length, endpoint);

                // 接收响应（带超时）
                var receiveTask = udp.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(HANDSHAKE_TIMEOUT_MS, ct)) != receiveTask)
                    return false;

                var result = receiveTask.Result;
                byte[] response = result.Buffer;

                // 验证响应头: MAGIC + CMD_HEARTBEAT + 长度(2) + 加密种子
                if (response.Length < 6 || response[0] != MAGIC_HANDSHAKE || response[1] != CMD_HEARTBEAT)
                    return false;

                int seedLen = (response[2] << 8) | response[3];
                if (response.Length < 4 + seedLen || seedLen != 256) // RSA 2048 → 256 bytes
                    return false;

                byte[] encryptedSeed = new byte[seedLen];
                Buffer.BlockCopy(response, 4, encryptedSeed, 0, seedLen);

                // 用私钥解密种子
                byte[] seed = _rsa.Decrypt(encryptedSeed, false);
                if (seed.Length != 32) return false;

                // 初始化加密器（与服务器一致）
                var cipher = new BeiShuiCipher();
                cipher.Init(seed);

                lock (_lock)
                {
                    _cipher = cipher;
                    _sequence = 0;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BAC-Client] 握手异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送加密心跳包
        /// </summary>
        private async Task<bool> SendEncryptedHeartbeatAsync(CancellationToken ct)
        {
            UdpClient? udp;
            IPEndPoint? endpoint;
            BeiShuiCipher? cipher;
            int seq;

            lock (_lock)
            {
                udp = _udpClient;
                endpoint = _serverEndpoint;
                cipher = _cipher;
                seq = _sequence++;
            }

            if (udp == null || endpoint == null || cipher == null)
                return false;

            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 构建心跳令牌（含设备指纹、玩家名、挑战响应）
                string? challengeResponse = null;
                if (_lastChallenge != null)
                {
                    byte[] resp = ComputeChallengeResponse(_lastChallenge, cipher);
                    challengeResponse = Convert.ToHexString(resp);
                }

                var token = new
                {
                    Status = !App.AntiCheatBlocked && App.AntiCheatPassed,
                    Version = $"BAC-{AntiCheat.Version}",
                    Timestamp = now,
                    Seq = seq,
                    Challenge = challengeResponse ?? "",
                    Fingerprint = GetMachineFingerprint(),
                    PlayerName = App.CurrentUser?.Username ?? "",
                };

                string json = JsonSerializer.Serialize(token);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                // 用 BeiShuiCipher 加密
                byte[] encrypted = cipher.Encrypt(jsonBytes);

                // 构建包: MAGIC + CMD_HEARTBEAT + SteamID(8) + 密文
                byte[] steamIdBytes = BitConverter.GetBytes(_localSteamId);
                byte[] packet = new byte[2 + 8 + encrypted.Length];
                packet[0] = MAGIC_HANDSHAKE;
                packet[1] = CMD_HEARTBEAT;
                Buffer.BlockCopy(steamIdBytes, 0, packet, 2, 8);
                Buffer.BlockCopy(encrypted, 0, packet, 10, encrypted.Length);

                await udp.SendAsync(packet, packet.Length, endpoint);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BAC-Client] 发送心跳异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理服务端的 Challenge（由外部在收到 challenge 时调用）
        /// </summary>
        public void SetChallenge(byte[] challenge)
        {
            _lastChallenge = challenge;
            _lastChallengeTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 计算 Challenge 响应：HMAC-SHA256(challenge, sessionKeyMaterial)
        /// </summary>
        private byte[] ComputeChallengeResponse(byte[] challenge, BeiShuiCipher cipher)
        {
            try
            {
                byte[]? keyMaterial = cipher.ExportSessionMaterial();
                if (keyMaterial == null)
                {
                    // 降级：使用机器指纹作为密钥
                    keyMaterial = Encoding.UTF8.GetBytes(GetMachineFingerprint());
                }
                using var hmac = new HMACSHA256(keyMaterial);
                return hmac.ComputeHash(challenge);
            }
            catch
            {
                // 最终降级
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("BeiShuiFallbackKey"));
                return hmac.ComputeHash(challenge);
            }
        }

        /// <summary>
        /// 获取客户端机器指纹（用于服务端识别唯一设备）
        /// </summary>
        public static string GetMachineFingerprint()
        {
            try
            {
                using var sha256 = SHA256.Create();

                // 组合多个硬件标识
                var components = new List<string>();

                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Cryptography");
                    if (key?.GetValue("MachineGuid") is string guid)
                        components.Add(guid);
                }
                catch { }

                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Control\SystemInformation");
                    if (key?.GetValue("SystemProductName") is string product)
                        components.Add(product);
                }
                catch { }

                components.Add(Environment.UserName);
                components.Add(Environment.MachineName);

                // 添加 MAC 地址
                try
                {
                    foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                        {
                            var mac = nic.GetPhysicalAddress();
                            if (mac != null && mac.ToString().Length > 0)
                            {
                                components.Add(mac.ToString());
                                break;
                            }
                        }
                    }
                }
                catch { }

                string combined = string.Join("|", components);
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToHexString(hash).Substring(0, 32);
            }
            catch
            {
                return "UNKNOWN-" + Environment.MachineName;
            }
        }

        /// <summary>
        /// 检查当前是否应该发送心跳（CS2 进程存在时）
        /// </summary>
        public static bool IsCs2Running()
        {
            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName("cs2");
                return procs.Length > 0;
            }
            catch { return false; }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Disconnect();
            _rsa?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
