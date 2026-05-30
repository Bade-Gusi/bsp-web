using System.Net;
using System.Net.Sockets;

namespace BeiShuiCS2.Services
{
    public static class NetworkHelper
    {
        /// <summary>
        /// 格式化主机地址：IPv6 加方括号，IPv4/域名不变
        /// </summary>
        public static string FormatHost(string host)
        {
            if (IPAddress.TryParse(host, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
                return $"[{host}]";
            return host;
        }

        /// <summary>
        /// 从 AppSettings 构建完整的 API BaseUrl
        /// </summary>
        public static string BuildApiBaseUrl()
        {
            var settings = AppSettings.Load();
            var scheme = settings.UseHttps ? "https" : "http";
            var host = FormatHost(settings.ServerUrl);
            return $"{scheme}://{host}:{settings.ServerPort}";
        }

        /// <summary>
        /// 从 AppSettings 构建 SignalR Hub URL（旧版单个 Hub 兼容）
        /// </summary>
        public static string BuildHubUrl()
        {
            var settings = AppSettings.Load();
            var scheme = settings.UseHttps ? "https" : "http";
            var host = FormatHost(settings.ServerUrl);
            return $"{scheme}://{host}:{settings.ServerPort}/callhub";
        }

        /// <summary>
        /// 从 AppSettings 构建 SignalR Hub 基础 URL（文档方案：多 Hub）
        /// </summary>
        public static string BuildHubBaseUrl()
        {
            var settings = AppSettings.Load();
            var scheme = settings.UseHttps ? "https" : "http";
            var host = FormatHost(settings.ServerUrl);
            return $"{scheme}://{host}:{settings.ServerPort}";
        }
    }
}
