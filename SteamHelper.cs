using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace BeiShuiCS2
{
    public static class SteamHelper
    {
        private static string? steamPath;
        private static string? steamInstallPath;
        private static string? _cs2Path;

        static SteamHelper()
        {
            // 尝试从注册表获取 Steam 安装路径
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key != null)
                {
                    steamInstallPath = key.GetValue("SteamPath") as string;
                    if (!string.IsNullOrEmpty(steamInstallPath))
                        steamPath = Path.Combine(steamInstallPath, "config");
                }
            }
            catch { }

            // 自动检测 CS2 路径
            _cs2Path = DetectCS2Path();
        }

        /// <summary>
        /// 自动检测 CS2 可执行文件路径
        /// </summary>
        public static string? DetectCS2Path()
        {
            // 1. 先尝试从已知配置中读取
            var settings = AppSettings.Load();
            if (!string.IsNullOrEmpty(settings.LastCS2Path) && File.Exists(settings.LastCS2Path))
                return settings.LastCS2Path;

            // 2. 从 Steam 注册表路径推断
            if (!string.IsNullOrEmpty(steamInstallPath))
            {
                // 标准 Steam 库路径
                string cs2Path = Path.Combine(steamInstallPath,
                    @"steamapps\common\Counter-Strike Global Offensive\cs2.exe");
                if (File.Exists(cs2Path))
                    return cs2Path;

                // CS2 新目录名称（有些机器用这个）
                string cs2Path2 = Path.Combine(steamInstallPath,
                    @"steamapps\common\CS2\game\bin\win64\cs2.exe");
                if (File.Exists(cs2Path2))
                    return cs2Path2;
            }

            // 3. 扫描常见 Steam 安装位置
            string[] commonPaths = new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\cs2.exe",
                @"C:\Program Files\Steam\steamapps\common\Counter-Strike Global Offensive\cs2.exe",
                @"D:\Steam\steamapps\common\Counter-Strike Global Offensive\cs2.exe",
                @"E:\Steam\steamapps\common\Counter-Strike Global Offensive\cs2.exe",
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // 4. 从 Steam libraryfolders.vdf 动态扫描所有库
            if (!string.IsNullOrEmpty(steamInstallPath))
            {
                string libraryFoldersPath = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(libraryFoldersPath))
                {
                    try
                    {
                        string content = File.ReadAllText(libraryFoldersPath, Encoding.UTF8);
                        // 匹配 "path"    "X:\\..."
                        var matches = Regex.Matches(content, @"path\s+""([^""]+)""");
                        foreach (Match match in matches)
                        {
                            string baseDir = match.Groups[1].Value.Replace(@"\\", @"\");
                            string[] searchDirs = new[]
                            {
                                Path.Combine(baseDir, @"steamapps\common\Counter-Strike Global Offensive\cs2.exe"),
                                Path.Combine(baseDir, @"steamapps\common\CS2\game\bin\win64\cs2.exe"),
                            };
                            foreach (var searchPath in searchDirs)
                            {
                                if (File.Exists(searchPath))
                                    return searchPath;
                            }
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取当前检测到的 CS2 路径（缓存）
        /// </summary>
        public static string? CS2Path => _cs2Path;

        /// <summary>
        /// 手动设置 CS2 路径并保存
        /// </summary>
        public static void SetCS2Path(string path)
        {
            _cs2Path = path;
            var settings = AppSettings.Load();
            settings.LastCS2Path = path;
            settings.Save();
        }

        /// <summary>
        /// 刷新 CS2 路径检测（重新扫描）
        /// </summary>
        public static string? RefreshCS2Path()
        {
            _cs2Path = DetectCS2Path();
            return _cs2Path;
        }

        /// <summary>
        /// 获取所有已登录的 Steam 用户
        /// </summary>
        public static List<SteamUser> GetLoggedInUsers()
        {
            var users = new List<SteamUser>();
            if (string.IsNullOrEmpty(steamPath)) return users;

            string loginUsersPath = Path.Combine(steamPath, "loginusers.vdf");
            if (!File.Exists(loginUsersPath)) return users;

            string content = File.ReadAllText(loginUsersPath, Encoding.UTF8);
            // 简单解析 VDF，提取用户信息
            var matches = Regex.Matches(content, @"""(\d+)""\s*{\s*""AccountName""\s*""([^""]+)""[^}]+""PersonaName""\s*""([^""]+)""", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    users.Add(new SteamUser
                    {
                        SteamID64 = match.Groups[1].Value,
                        AccountName = match.Groups[2].Value,
                        PersonaName = match.Groups[3].Value,
                        // 头像需要从网络下载，这里暂用默认
                        Avatar = null
                    });
                }
            }
            return users;
        }

        /// <summary>
        /// 获取头像缓存路径
        /// </summary>
        private static string GetAvatarCachePath(string steamID)
        {
            string avatarCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BeiShuiCS2", "AvatarCache");
            Directory.CreateDirectory(avatarCachePath);
            return Path.Combine(avatarCachePath, $"{steamID}.jpg");
        }

        /// <summary>
        /// 获取Steam用户头像（仅从本地缓存加载，不阻塞网络。返回的 BitmapImage 已 Freeze）
        /// </summary>
        public static BitmapImage? GetAvatar(string steamID)
        {
            try
            {
                string avatarFile = GetAvatarCachePath(steamID);
                if (File.Exists(avatarFile))
                    return LoadCachedBitmap(avatarFile);
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 异步下载 Steam 头像到本地缓存（可在后台线程调用）
        /// </summary>
        public static async System.Threading.Tasks.Task DownloadAvatarAsync(string steamID)
        {
            try
            {
                string avatarFile = GetAvatarCachePath(steamID);
                if (File.Exists(avatarFile)) return; // 已有缓存

                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                string apiUrl = $"https://steamcommunity.com/profiles/{steamID}/?xml=1";

                string response = await client.GetStringAsync(apiUrl).ConfigureAwait(false);
                var avatarMatch = System.Text.RegularExpressions.Regex.Match(response,
                    @"<avatarFull><!\[CDATA\[(.*?)\]\]></avatarFull>");

                if (avatarMatch.Success)
                {
                    string avatarUrl = avatarMatch.Groups[1].Value;
                    byte[] imageData = await client.GetByteArrayAsync(avatarUrl).ConfigureAwait(false);
                    File.WriteAllBytes(avatarFile, imageData);
                }
            }
            catch { }
        }

        private static BitmapImage LoadCachedBitmap(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            // 冻结后可在任意线程使用
            if (bitmap.CanFreeze) bitmap.Freeze();
            return bitmap;
        }

    }

    public class SteamUser
    {
        public string SteamID64 { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string PersonaName { get; set; } = "";
        public BitmapImage? Avatar { get; set; }
    }
}
