using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace BeiShuiCS2.Services
{
    public class AuthService
    {
        private const string SessionFileName = "session.json";
        private string SessionFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SessionFileName);

        public string? Token { get; private set; }
        public DateTime? TokenExpiresAt { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        public bool IsTokenExpired => TokenExpiresAt.HasValue && DateTime.Now >= TokenExpiresAt.Value;

        public event Action? OnTokenRefreshed;
        public event Action? OnLoggedOut;

        public void SaveToken(string token, DateTime expiresAt)
        {
            Token = token;
            TokenExpiresAt = expiresAt;
            try
            {
                var session = new { Token = token, ExpiresAt = expiresAt };
                var json = JsonSerializer.Serialize(session);
                File.WriteAllText(SessionFilePath, json);
            }
            catch { /* 静默处理文件写入失败 */ }
        }

        public string? LoadToken()
        {
            try
            {
                if (!File.Exists(SessionFilePath)) return null;
                var json = File.ReadAllText(SessionFilePath);
                var session = JsonSerializer.Deserialize<SessionData>(json);
                if (session == null) return null;
                Token = session.Token;
                TokenExpiresAt = session.ExpiresAt;
                if (IsTokenExpired)
                {
                    ClearToken();
                    return null;
                }
                return Token;
            }
            catch
            {
                return null;
            }
        }

        public void ClearToken()
        {
            Token = null;
            TokenExpiresAt = null;
            try
            {
                if (File.Exists(SessionFilePath))
                    File.Delete(SessionFilePath);
            }
            catch { }
            OnLoggedOut?.Invoke();
        }

        public async Task<string?> RefreshTokenAsync(string refreshEndpoint = "/api/auth/refresh")
        {
            try
            {
                var response = await ApiClient.PostAsync<RefreshResponse>(refreshEndpoint, new { });
                if (response.Success && response.Data != null)
                {
                    if (response.Data.Token != null)
                    {
                        SaveToken(response.Data.Token, DateTime.Now.AddDays(7));
                        OnTokenRefreshed?.Invoke();
                        return response.Data.Token;
                    }
                }
            }
            catch { }
            return null;
        }

        private class SessionData
        {
            public string? Token { get; set; }
            public DateTime ExpiresAt { get; set; }
        }

        private class RefreshResponse
        {
            public string? Token { get; set; }
        }
    }
}
