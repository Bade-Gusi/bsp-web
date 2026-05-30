using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeiShuiCS2.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public string BaseUrl { get; private set; }

        public ApiService(string baseUrl)
        {
            BaseUrl = baseUrl.TrimEnd('/');
            _http = CreateHttpClient();
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            return new HttpClient(handler);
        }

        public void SetToken(string? token)
        {
            _http.DefaultRequestHeaders.Authorization = token != null
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? body = null)
        {
            try
            {
                var json = body != null ? JsonSerializer.Serialize(body, _jsonOptions) : "{}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync($"{BaseUrl}{endpoint}", content);
                var respJson = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return new ApiResponse<T>
                    {
                        Success = true,
                        Data = JsonSerializer.Deserialize<T>(respJson, _jsonOptions)
                    };
                var err = JsonSerializer.Deserialize<ApiError>(respJson, _jsonOptions);
                return new ApiResponse<T> { Success = false, Error = err?.Error ?? "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Error = $"网络错误: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _http.GetAsync($"{BaseUrl}{endpoint}");
                var respJson = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return new ApiResponse<T>
                    {
                        Success = true,
                        Data = JsonSerializer.Deserialize<T>(respJson, _jsonOptions)
                    };
                return new ApiResponse<T> { Success = false, Error = "请求失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T> { Success = false, Error = $"网络错误: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<object>> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _http.DeleteAsync($"{BaseUrl}{endpoint}");
                if (response.IsSuccessStatusCode)
                    return new ApiResponse<object> { Success = true };
                return new ApiResponse<object> { Success = false, Error = "删除失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object> { Success = false, Error = $"网络错误: {ex.Message}" };
            }
        }
    }

    public class ApiError
    {
        public string? Error { get; set; }
    }
}
