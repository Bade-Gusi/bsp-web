using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeiShuiCS2
{
    public class AntiCheatToken
    {
        public string SteamID { get; set; } = "";
        public bool Status { get; set; }
        public string Version { get; set; } = "";
        public long Timestamp { get; set; }
        public int Seq { get; set; }           // 新增：序列号，防重放
        public string Signature { get; set; } = "";

        // 必须与服务器端 LegacySecretKey 完全一致
        private const string SecretKey = "1M@r#9v$thfhf?346+2qL!pX7nY&3zR";

        // 每个 SteamID 的序列号（简单实现，实际可用静态字典，但通常只有一个玩家）
        private static int _seqCounter = 0;

        public static AntiCheatToken Create(string steamId, bool status, string version)
        {
            // 每次生成 Token 时递增序列号
            int seq = System.Threading.Interlocked.Increment(ref _seqCounter);
            var token = new AntiCheatToken
            {
                SteamID = steamId,
                Status = status,
                Version = version,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Seq = seq
            };
            token.Signature = ComputeSignature(token);
            return token;
        }

        // 可选：重置序列号（当玩家断开连接或重新登录时调用）
        public static void ResetSequence() => _seqCounter = 0;

        private static string ComputeSignature(AntiCheatToken token)
        {
            // 注意：签名数据包含 Seq 字段
            string data = $"{token.SteamID}|{token.Status}|{token.Version}|{token.Timestamp}|{token.Seq}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        public string ToJson()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return JsonSerializer.Serialize(this, options);
        }
    }
}