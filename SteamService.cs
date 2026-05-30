using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace BeiShuiCS2
{
    /// <summary>
    /// Steam 用户信息类
    /// </summary>
    public class SteamUserInfo
    {
        public string AccountName { get; set; } = "";
        public string PersonaName { get; set; } = "";
        public string SteamID64 { get; set; } = "";
        public string AvatarPath { get; set; } = "";
    }

    public static class SteamService
    {
        private static SteamUserInfo? _selectedSteamUser = null; // 缓存已选择的用户

        /// <summary>
        /// 获取当前要使用的 Steam 用户（如果多个则弹窗选择，并缓存结果）
        /// </summary>
        public static SteamUserInfo? GetCurrentSteamUser()
        {
            // 如果已经选择过，直接返回
            if (_selectedSteamUser != null)
                return _selectedSteamUser;

            try
            {
                string steamPath = GetSteamPath();
                if (string.IsNullOrEmpty(steamPath))
                    return null;

                string vdfPath = Path.Combine(steamPath, "config", "loginusers.vdf");
                if (!File.Exists(vdfPath))
                    return null;

                string content = File.ReadAllText(vdfPath, Encoding.UTF8);
                var users = ParseAllUsers(content);

                if (users.Count == 0)
                    return null;

                if (users.Count == 1)
                {
                    _selectedSteamUser = users[0];
                }
                else
                {
                    // 多个用户，弹出选择窗口（确保只在主线程调用）
                    _selectedSteamUser = Application.Current.Dispatcher.Invoke(() =>
                    {
                        var dialog = new SteamUserSelectionDialog(users);
                        if (dialog.ShowDialog() == true)
                        {
                            return dialog.SelectedUser;
                        }
                        return null;
                    });
                }

                // 如果成功选择，异步下载头像
                if (_selectedSteamUser != null)
                {
                    _ = DownloadAvatarAsync(steamPath, _selectedSteamUser);
                }

                return _selectedSteamUser;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 清除已选择的用户（例如退出登录时调用）
        /// </summary>
        public static void ClearSelectedUser()
        {
            _selectedSteamUser = null;
        }

        /// <summary>
        /// 从注册表获取 Steam 安装路径
        /// </summary>
        public static string GetSteamPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return key?.GetValue("SteamPath")?.ToString() ?? "";
            }
            catch { return ""; }
        }

        /// <summary>
        /// 解析 loginusers.vdf 中的所有用户
        /// </summary>
        private static List<SteamUserInfo> ParseAllUsers(string vdfContent)
        {
            var users = new List<SteamUserInfo>();

            // 使用正则匹配所有 SteamID64 块
            // 格式："7656119xxxxxxxx" { "AccountName" "xxx" "PersonaName" "xxx" ... }
            var matches = Regex.Matches(vdfContent, @"""(\d{17})""\s*{\s*""AccountName""\s*""([^""]+)""[^}]+""PersonaName""\s*""([^""]+)""", RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    users.Add(new SteamUserInfo
                    {
                        SteamID64 = match.Groups[1].Value,
                        AccountName = match.Groups[2].Value,
                        PersonaName = match.Groups[3].Value
                    });
                }
            }
            return users;
        }

        /// <summary>
        /// 异步下载用户头像
        /// </summary>
        private static async System.Threading.Tasks.Task DownloadAvatarAsync(string steamPath, SteamUserInfo user)
        {
            // 尝试从本地缓存获取
            string[] possiblePaths = new[]
            {
                Path.Combine(steamPath, "config", "avatarcache", $"{user.SteamID64}.png"),
                Path.Combine(steamPath, "config", "avatarcache", $"{user.SteamID64}.jpg"),
                Path.Combine(steamPath, "appcache", "librarycache", $"{user.SteamID64}.jpg")
            };

            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    user.AvatarPath = path;
                    return;
                }
            }

            // 如果本地没有，从网络下载（可选）
            try
            {
                using var client = new System.Net.Http.HttpClient();
                string avatarUrl = $"https://avatars.steamstatic.com/{user.SteamID64}_full.jpg";
                byte[] data = await client.GetByteArrayAsync(avatarUrl).ConfigureAwait(false);

                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BeiShuiCS2", "avatars");
                if (!System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.CreateDirectory(tempDir);

                string tempFile = System.IO.Path.Combine(tempDir, $"{user.SteamID64}.jpg");
                await System.IO.File.WriteAllBytesAsync(tempFile, data).ConfigureAwait(false);
                user.AvatarPath = tempFile;
            }
            catch
            {
                // 下载失败，保持头像路径为空
            }
        }
    }

    /// <summary>
    /// Steam 用户选择对话框（简单实现）
    /// </summary>
    public class SteamUserSelectionDialog : Window
    {
        private List<SteamUserInfo> _users;
        public SteamUserInfo? SelectedUser { get; private set; }

        public SteamUserSelectionDialog(List<SteamUserInfo> users)
        {
            _users = users;
            Title = "选择 Steam 账号";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.White;

            var stack = new StackPanel { Margin = new Thickness(10) };
            var listBox = new System.Windows.Controls.ListBox { DisplayMemberPath = "PersonaName", Height = 200 };
            listBox.ItemsSource = _users;
            listBox.SelectedIndex = 0;

            var btnOk = new System.Windows.Controls.Button { Content = "确定", Width = 100, Height = 30, Margin = new Thickness(0, 10, 0, 0) };
            btnOk.Click += (s, e) =>
            {
                SelectedUser = listBox.SelectedItem as SteamUserInfo;
                if (SelectedUser != null)
                    DialogResult = true;
                else
                    MessageBox.Show("请选择一个账号");
            };

            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = "检测到多个 Steam 账号，请选择要使用的账号：" });
            stack.Children.Add(listBox);
            stack.Children.Add(btnOk);
            Content = stack;
        }
    }
}