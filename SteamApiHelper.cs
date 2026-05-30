using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BeiShuiCS2
{
    public static class SteamApiHelper
    {
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15) // 全局 HttpClient 超时
        };

        /// <summary>
        /// 根据 SteamID 或自定义URL获取用户信息（带超时控制）
        /// </summary>
        public static async Task<(string? name, string? avatarUrl, string? steamID)?> GetUserInfoAsync(string input)
        {
            // 创建一个链接源，用于手动控制超时
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10秒超时

            try
            {
                string url;
                if (long.TryParse(input, out _))
                {
                    url = $"https://steamcommunity.com/profiles/{input}/?xml=1";
                }
                else
                {
                    url = $"https://steamcommunity.com/id/{input}/?xml=1";
                }

                // 传递 CancellationToken
                string xml = await httpClient.GetStringAsync(url, cts.Token).ConfigureAwait(false);
                var doc = XDocument.Parse(xml);
                var root = doc.Root;

                if (root == null) return null;

                var error = root.Element("error");
                if (error != null) return null;

                string? name = root.Element("steamID")?.Value;
                string? avatarUrl = root.Element("avatarFull")?.Value ?? root.Element("avatarMedium")?.Value ?? root.Element("avatarIcon")?.Value;
                string? steamID = root.Element("steamID64")?.Value;

                return (name, avatarUrl, steamID);
            }
            catch (OperationCanceledException)
            {
                // 超时异常
                throw new TimeoutException("Steam API 请求超时，请稍后重试。");
            }
            catch (HttpRequestException ex)
            {
                // 网络异常
                throw new Exception($"网络请求失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                // 其他异常（如 XML 解析错误）
                throw new Exception($"数据解析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 下载头像到临时目录并返回本地路径（带超时控制）
        /// </summary>
        public static async Task<string?> DownloadAvatarAsync(string avatarUrl, string steamID)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            try
            {
                byte[] data = await httpClient.GetByteArrayAsync(avatarUrl, cts.Token).ConfigureAwait(false);
                string tempDir = Path.Combine(Path.GetTempPath(), "BeiShuiCS2", "avatars");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                string ext = Path.GetExtension(avatarUrl)?.Split('?')[0] ?? ".jpg";
                string fileName = $"{steamID}{ext}";
                string filePath = Path.Combine(tempDir, fileName);
                await File.WriteAllBytesAsync(filePath, data, cts.Token).ConfigureAwait(false);
                return filePath;
            }
            catch
            {
                return null;
            }
        }
    }
}