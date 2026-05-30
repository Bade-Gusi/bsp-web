using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeiShuiCS2
{
    public static class ApiClient
    {
        private static HttpClient _http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            // IPv6 优先：设置 Happy Eyeballs 超时短一些
            handler.Properties["HttpClientHandler.MaxConnectionsPerServer"] = 10;
            return new HttpClient(handler);
        }
        private static string _baseUrl = "http://127.0.0.1:5000";

        public static void SetBaseUrl(string url) => _baseUrl = url.TrimEnd('/');

        public static void SetToken(string? token)
        {
            _http.DefaultRequestHeaders.Authorization = token != null
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;
        }

        public static async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object body)
        {
            try
            {
                var json = JsonSerializer.Serialize(body, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync($"{_baseUrl}{endpoint}", content);
                var respJson = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return new ApiResponse<T> { Success = true, Data = JsonSerializer.Deserialize<T>(respJson, JsonOptions) };
                var err = JsonSerializer.Deserialize<ApiError>(respJson, JsonOptions);
                return new ApiResponse<T> { Success = false, Error = err?.Error ?? "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Error = $"网络错误: {ex.Message}" };
            }
        }

        public static async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}{endpoint}");
                var respJson = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return new ApiResponse<T> { Success = true, Data = JsonSerializer.Deserialize<T>(respJson, JsonOptions) };
                return new ApiResponse<T> { Success = false, Error = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Error = $"网络错误: {ex.Message}" };
            }
        }

        public static async Task<ApiResponse<object>> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _http.DeleteAsync($"{_baseUrl}{endpoint}");
                if (response.IsSuccessStatusCode)
                    return new ApiResponse<object> { Success = true };
                return new ApiResponse<object> { Success = false, Error = "删除失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object> { Success = false, Error = $"网络错误: {ex.Message}" };
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
    }

    public class ApiError
    {
        public string? Error { get; set; }
    }

    public class MatchHistoryItem
    {
        public string? Map { get; set; }
        public string? Result { get; set; }
        public string? Score { get; set; }
        public string PlayedAt { get; set; } = "";
    }

    public class FriendItem
    {
        public int FriendId { get; set; }
        public string FriendName { get; set; } = "";
        public bool IsOnline { get; set; }
    }

    public class RoomItem
    {
        public string RoomId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Map { get; set; } = "";
        public string Mode { get; set; } = "";
        public int MaxPlayers { get; set; }
        public string HostName { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class LeaderboardItem
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public int Elo { get; set; }
    }
}
