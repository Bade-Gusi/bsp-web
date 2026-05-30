using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeiShuiCS2
{
    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public bool IsUpdateAvailable { get; set; }
    }

    public static class UpdateChecker
    {
        public const string Owner = "Bade-Gusi";
        public const string Repo = "BeiShuiCS2";
        private static readonly string CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly()
            .GetName()?.Version?.ToString() ?? "2.0.0";

        /// <summary>
        /// 检查 GitHub Releases 是否有新版本
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdates()
        {
            var info = new UpdateInfo();
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("BeiShuiCS2");
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    string tagName = root.GetProperty("tag_name").GetString() ?? "";
                    string body = root.GetProperty("body").GetString() ?? "";

                    if (CompareVersions(tagName.TrimStart('v'), CurrentVersion) > 0)
                    {
                        info.LatestVersion = tagName;
                        info.ReleaseNotes = body;
                        info.IsUpdateAvailable = true;

                        var assets = root.GetProperty("assets");
                        foreach (var asset in assets.EnumerateArray())
                        {
                            string name = asset.GetProperty("name").GetString() ?? "";
                            if (name.EndsWith(".exe") || name.EndsWith(".zip"))
                            {
                                info.DownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // 静默失败，更新检查非关键
            }
            return info;
        }

        /// <summary>
        /// 下载更新包到临时目录
        /// </summary>
        public static async Task<string?> DownloadUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "BeiShuiCS2_Update");
                Directory.CreateDirectory(tempDir);

                string zipPath = Path.Combine(tempDir, "update.zip");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("BeiShuiCS2");
                client.Timeout = TimeSpan.FromMinutes(5);

                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                long bytesRead = 0;
                int bytes;
                while ((bytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytes);
                    bytesRead += bytes;
                    if (totalBytes > 0 && progress != null)
                        progress.Report((int)(bytesRead * 100 / totalBytes));
                }

                return zipPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 安装更新：解压 -> 创建重启脚本 -> 启动脚本 -> 退出当前进程
        /// </summary>
        public static void InstallUpdate(string zipPath)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "BeiShuiCS2_Update");
                string extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                // 解压更新包
                ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string updaterScript = Path.Combine(tempDir, "update.cmd");

                // 创建更新批处理脚本
                using (var sw = new StreamWriter(updaterScript, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("@echo off");
                    sw.WriteLine("chcp 65001 >nul");
                    sw.WriteLine("title 背水对战平台 - 更新中");
                    sw.WriteLine("echo 正在更新，请稍候...");
                    sw.WriteLine("");
                    sw.WriteLine(":wait");
                    sw.WriteLine($"tasklist /FI \"IMAGENAME eq BeiShuiCS2.exe\" 2>nul | find /I \"BeiShuiCS2.exe\" >nul");
                    sw.WriteLine("if %errorlevel% equ 0 (");
                    sw.WriteLine("    timeout /t 1 /nobreak >nul");
                    sw.WriteLine("    goto wait");
                    sw.WriteLine(")");
                    sw.WriteLine("");
                    sw.WriteLine($"echo 正在复制更新文件...");
                    sw.WriteLine($"xcopy /E /Y /Q \"{extractDir}\\*.*\" \"{baseDir}\" >nul");
                    sw.WriteLine("");
                    sw.WriteLine("echo 更新完成，正在启动...");
                    sw.WriteLine($"start \"\" \"{baseDir}BeiShuiCS2.exe\"");
                    sw.WriteLine("exit");
                }

                // 启动更新脚本
                var psi = new ProcessStartInfo
                {
                    FileName = updaterScript,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                Process.Start(psi);

                // 退出当前程序
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InstallUpdate failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 打开下载链接让用户手动下载
        /// </summary>
        public static void OpenDownloadPage(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private static int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');
            int max = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < max; i++)
            {
                int p1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
                int p2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;
                if (p1 != p2) return p1.CompareTo(p2);
            }
            return 0;
        }
    }
}
