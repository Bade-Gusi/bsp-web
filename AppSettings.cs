using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BeiShuiCS2
{
    public class AppSettings
    {
        private static readonly SemaphoreSlim _saveLock = new(1, 1);

        // 窗口设置
        public double MainWindowLeft { get; set; }
        public double MainWindowTop { get; set; }
        public double MainWindowWidth { get; set; }
        public double MainWindowHeight { get; set; }
        public bool IsMainWindowMaximized { get; set; }

        // 登录记忆
        public string LastUsername { get; set; } = "";
        public bool RememberPassword { get; set; } = false;

        // 匹配偏好
        public string LastSelectedMap { get; set; } = "炼狱小镇";
        public string LastSelectedServerRegion { get; set; } = "亚洲 - 香港";
        public string LastSelectedServerIP { get; set; } = "";

        // 其他设置
        public bool AutoConnectAfterMatch { get; set; } = true;
        public bool ShowToastNotifications { get; set; } = true;

        // 是否首次启动
        public bool IsFirstLaunch { get; set; } = true;

        // 新增：启动时自动创建桌面快捷方式
        public bool AutoCreateShortcut { get; set; } = true;

        // 广播历史
        public string BroadcastHistory { get; set; } = "";

        // CS2 路径
        public string LastCS2Path { get; set; } = "";

        // 启动设置
        public bool AutoStartWithSystem { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public string Language { get; set; } = "zh-CN";
        public string LaunchArgs { get; set; } = "-novid -high";

        // ===== 通用设置 =====
        public string DownloadPath { get; set; } = "";
        public bool AutoUpdate { get; set; } = true;

        // ===== 显示设置 =====
        public double UiScale { get; set; } = 1.0;
        public bool AnimationsEnabled { get; set; } = true;
        public string AnimationSpeed { get; set; } = "medium";
        public bool CollapseSidebar { get; set; } = false;
        public string Theme { get; set; } = "dark";

        // ===== 隐私设置 =====
        public bool AllowDataCollection { get; set; } = false;
        public bool SendErrorReports { get; set; } = true;
        public bool InvisibleMode { get; set; } = false;

        // ===== 安全设置 =====
        public bool LoginNotifications { get; set; } = true;

        // ===== 服务器连接设置 =====
        public string ServerUrl { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 5000;
        public bool UseHttps { get; set; } = false;
        public bool IPv6AutoEnable { get; set; } = true;
        public bool IPv6Checked { get; set; } // 是否已检查过 IPv6

        // ===== 通知设置 =====
        public bool MatchNotifications { get; set; } = true;
        public bool FriendNotifications { get; set; } = true;
        public bool SystemNotifications { get; set; } = true;
        public bool DndEnabled { get; set; } = false;
        public string DndStartTime { get; set; } = "22:00";
        public string DndEndTime { get; set; } = "08:00";

        // ===== 节日通知 =====
        public string LastDismissedHoliday { get; set; } = "";

        public static AppSettings Load()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch { }
            }
            return new AppSettings();
        }

        public void Save()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            try
            {
                _saveLock.Wait();
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
            finally
            {
                _saveLock.Release();
            }
        }
    }
}