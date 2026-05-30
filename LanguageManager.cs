using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BeiShuiCS2
{
    public static class LanguageManager
    {
        private static string _currentLanguage = "zh-CN";
        private static readonly Dictionary<string, Dictionary<string, string>> _packs = new();

        /// <summary>
        /// 语言切换时触发，窗口订阅此事件刷新界面文本
        /// </summary>
        public static event Action? LanguageChanged;

        public static string CurrentLanguage => _currentLanguage;

        static LanguageManager()
        {
            // ===== 简体中文 =====
            _packs["zh-CN"] = new Dictionary<string, string>
            {
                ["LangChanged"] = "语言已切换",
                ["Loading"] = "加载中...",
                ["PlatformName"] = "背水对战平台",
                ["Online"] = "在线",
                ["Offline"] = "离线",
                ["Home"] = "首页",
                ["QuickMatch"] = "快速匹配",
                ["Duel"] = "1v1 对战",
                ["CreateServer"] = "开服务器",
                ["RoomHall"] = "房间大厅",
                ["Server"] = "服务器",
                ["Friends"] = "好友",
                ["Chat"] = "聊天",
                ["History"] = "战绩",
                ["Demo"] = "Demo回放",
                ["Leaderboard"] = "排行榜",
                ["Achievements"] = "成就",
                ["SkinMarket"] = "皮肤市场",
                ["MatchingQueue"] = "匹配队列",
                ["MiniGames"] = "小游戏",
                ["VoiceCall"] = "语音通话",
                ["ScreenShare"] = "屏幕分享",
                ["Settings"] = "设置",
                ["Logout"] = "退出登录",
                ["Close"] = "关闭",
                ["Save"] = "保存",
                ["Cancel"] = "取消",
                ["Confirm"] = "确认",
                ["Delete"] = "删除",
                ["Edit"] = "编辑",
                ["Search"] = "搜索",
                ["NoData"] = "暂无数据",
                ["LoadingData"] = "正在加载平台数据...",
                ["PreloadModules"] = "正在预加载功能模块...",
                ["Ready"] = "准备就绪",
                ["DeviceManagement"] = "已登录设备",
                ["CurrentDevice"] = "当前设备",
                ["Language"] = "显示语言",
                ["Theme"] = "主题",
                ["UiScale"] = "界面缩放",
                ["DuelMatch"] = "随机匹配",
                ["DuelFriend"] = "好友对战",
                ["StartMatch"] = "开始匹配",
                ["InviteDuel"] = "邀约对战",
            };

            // ===== English =====
            _packs["en"] = new Dictionary<string, string>
            {
                ["LangChanged"] = "Language switched",
                ["Loading"] = "Loading...",
                ["PlatformName"] = "BeiShui Platform",
                ["Online"] = "Online",
                ["Offline"] = "Offline",
                ["Home"] = "Home",
                ["QuickMatch"] = "Quick Match",
                ["Duel"] = "1v1 Duel",
                ["CreateServer"] = "Host Server",
                ["RoomHall"] = "Rooms",
                ["Server"] = "Servers",
                ["Friends"] = "Friends",
                ["Chat"] = "Chat",
                ["History"] = "History",
                ["Demo"] = "Demo Player",
                ["Leaderboard"] = "Leaderboard",
                ["Achievements"] = "Achievements",
                ["SkinMarket"] = "Skin Market",
                ["MatchingQueue"] = "Match Queue",
                ["MiniGames"] = "Mini Games",
                ["VoiceCall"] = "Voice Call",
                ["ScreenShare"] = "Share Screen",
                ["Settings"] = "Settings",
                ["Logout"] = "Logout",
                ["Close"] = "Close",
                ["Save"] = "Save",
                ["Cancel"] = "Cancel",
                ["Confirm"] = "Confirm",
                ["Delete"] = "Delete",
                ["Edit"] = "Edit",
                ["Search"] = "Search",
                ["NoData"] = "No data available",
                ["LoadingData"] = "Loading platform data...",
                ["Ready"] = "Ready",
                ["DeviceManagement"] = "Device Management",
                ["CurrentDevice"] = "Current Device",
                ["Language"] = "Language",
                ["Theme"] = "Theme",
                ["UiScale"] = "UI Scale",
                ["DuelMatch"] = "Random Match",
                ["DuelFriend"] = "Friend Duel",
                ["StartMatch"] = "Start Match",
                ["InviteDuel"] = "Invite to Duel",
            };
        }

        public static void SetLanguage(string langCode)
        {
            if (_packs.ContainsKey(langCode))
            {
                _currentLanguage = langCode;
                LanguageChanged?.Invoke();
            }
        }

        public static string GetString(string key)
        {
            if (_packs.TryGetValue(_currentLanguage, out var pack) && pack.TryGetValue(key, out var value))
                return value;
            // Fallback to zh-CN
            if (_currentLanguage != "zh-CN" && _packs.TryGetValue("zh-CN", out var zhPack) && zhPack.TryGetValue(key, out var zhValue))
                return zhValue;
            return key;
        }

        public static bool HasLanguage(string langCode) => _packs.ContainsKey(langCode);
        public static IEnumerable<string> GetAvailableLanguages() => _packs.Keys.ToList();
    }
}
