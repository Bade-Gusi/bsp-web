using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BeiShuiCS2
{
    /// <summary>
    /// 背水自定义加密器 — 与服务器 BAC 插件完全一致的实现
    /// 使用 32 字节种子初始化 S 盒 + 掩码，支持 Encrypt/Decrypt
    /// </summary>
    public class BeiShuiCipher
    {
        private byte[] _sBox = null!;
        private byte[] _invSBox = null!;
        private byte[] _mask = null!;
        private byte[]? _sessionMaterial;

        public void Init(byte[] seed)
        {
            if (seed.Length != 32) throw new ArgumentException("Seed must be 32 bytes");
            _sessionMaterial = (byte[])seed.Clone();

            var prng = new CustomPRNG(seed);
            _sBox = new byte[256];
            for (int i = 0; i < 256; i++) _sBox[i] = (byte)i;
            for (int i = 255; i > 0; i--)
            {
                int j = prng.Next() % (i + 1);
                (_sBox[i], _sBox[j]) = (_sBox[j], _sBox[i]);
            }

            _invSBox = new byte[256];
            for (int i = 0; i < 256; i++) _invSBox[_sBox[i]] = (byte)i;

            _mask = new byte[16];
            prng.ReadBytes(_mask, 0, 16);
        }

        /// <summary>
        /// 导出会话密钥材料（用于挑战-响应 HMAC）
        /// </summary>
        public byte[]? ExportSessionMaterial() => _sessionMaterial;

        public byte[] Encrypt(byte[] plain)
        {
            byte[] salt = new byte[4];
            RandomNumberGenerator.Fill(salt);

            byte[] checksum = ComputeChecksum(plain);
            byte[] plainWithMeta = new byte[4 + plain.Length + 4];
            Buffer.BlockCopy(salt, 0, plainWithMeta, 0, 4);
            Buffer.BlockCopy(plain, 0, plainWithMeta, 4, plain.Length);
            Buffer.BlockCopy(checksum, 0, plainWithMeta, 4 + plain.Length, 4);

            byte[] cipher = new byte[plainWithMeta.Length];
            for (int i = 0; i < plainWithMeta.Length; i++)
            {
                byte b = plainWithMeta[i];
                b = (byte)(b ^ _mask[i % 16]);
                b = _sBox[b];
                b = (byte)((b << 3) | (b >> 5));
                b = (byte)(b ^ (i & 0xFF));
                cipher[i] = b;
            }
            return cipher;
        }

        public byte[]? Decrypt(byte[] cipher)
        {
            byte[] dec = new byte[cipher.Length];
            for (int i = 0; i < cipher.Length; i++)
            {
                byte b = cipher[i];
                b = (byte)(b ^ (i & 0xFF));
                b = (byte)((b >> 3) | (b << 5));
                b = _invSBox[b];
                b = (byte)(b ^ _mask[i % 16]);
                dec[i] = b;
            }

            if (dec.Length < 8) return null;

            byte[] data = new byte[dec.Length - 8];
            Buffer.BlockCopy(dec, 4, data, 0, dec.Length - 8);

            byte[] receivedChecksum = new byte[4];
            Buffer.BlockCopy(dec, dec.Length - 4, receivedChecksum, 0, 4);

            byte[] computedChecksum = ComputeChecksum(data);
            for (int i = 0; i < 4; i++)
            {
                if (computedChecksum[i] != receivedChecksum[i])
                    return null;
            }

            return data;
        }

        private byte[] ComputeChecksum(byte[] data)
        {
            uint sum = 0;
            byte xor = 0;
            foreach (byte b in data)
            {
                sum += b;
                xor ^= b;
            }
            return new byte[]
            {
                (byte)(sum & 0xFF),
                (byte)((sum >> 8) & 0xFF),
                (byte)((sum >> 16) & 0xFF),
                xor
            };
        }

        private class CustomPRNG
        {
            private uint _state;
            public CustomPRNG(byte[] seed)
            {
                _state = BitConverter.ToUInt32(seed, 0) ^ 0xDEADBEEF;
                for (int i = 4; i < seed.Length; i += 4)
                    _state ^= BitConverter.ToUInt32(seed, i);
            }

            public int Next()
            {
                _state = _state * 1103515245 + 12345;
                return (int)((_state >> 16) & 0x7FFF);
            }

            public void ReadBytes(byte[] buf, int offset, int len)
            {
                for (int i = 0; i < len; i++)
                    buf[offset + i] = (byte)Next();
            }
        }
    }
}
