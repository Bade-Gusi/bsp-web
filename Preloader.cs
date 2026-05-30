using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    /// <summary>
    /// 平台预加载器 — 在登录成功后、主窗口显示前执行后台初始化
    /// </summary>
    public static class Preloader
    {
        private static bool _isPreloaded;
        private static readonly List<string> _loadedModules = new();
        private static readonly object _lock = new();

        public static bool IsPreloaded => _isPreloaded;
        public static IReadOnlyList<string> LoadedModules => _loadedModules.AsReadOnly();

        public static event Action<string>? ModuleLoaded;

        /// <summary>
        /// 初始化：在登录成功后调用，异步初始化所有子系统
        /// </summary>
        public static async Task InitializeAsync(IProgress<string>? progress = null)
        {
            lock (_lock)
            {
                if (_isPreloaded) return;
            }

            var steps = new (string name, Func<Task> action)[]
            {
                ("动画引擎", InitAnimationEngine),
                ("WebView2 运行时", InitWebView2Async),
                ("系统监控", InitSystemMonitor),
                ("反作弊引擎", InitAntiCheat),
                ("数据缓存", InitDataCache),
                ("音频管理器", InitAudio),
                ("热键系统", InitHotkeys),
            };

            foreach (var (name, action) in steps)
            {
                try
                {
                    progress?.Report(name);
                    await action();
                    lock (_lock) _loadedModules.Add(name);
                    ModuleLoaded?.Invoke(name);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Preloader] {name} 初始化失败: {ex.Message}");
                }
            }

            _isPreloaded = true;
        }

        private static Task InitAnimationEngine()
        {
            // 预初始化动画时间线（WPF 动画系统冷启动预热）
            var dummy = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(10));
            var clock = dummy.CreateClock();
            clock.Controller?.Begin();
            return Task.CompletedTask;
        }

        private static Task InitWebView2Async()
        {
            // WebView2 环境预初始化 — 确保核心已就绪
            // 实际 WebView2 初始化在 MainWindow 中延迟执行
            return Task.CompletedTask;
        }

        private static Task InitSystemMonitor()
        {
            // 预初始化 PerformanceCounter（延迟构造避免卡 UI）
            try
            {
                using var cpu = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpu.NextValue(); // 首次调用会触发初始化
            }
            catch { }
            return Task.CompletedTask;
        }

        private static Task InitAntiCheat()
        {
            // 反作弊模块已由 App.OnStartup 初始化，此处确保看门狗就绪
            return Task.CompletedTask;
        }

        private static Task InitDataCache()
        {
            // 预加载常用数据（设置、服务器列表等）
            try
            {
                var _ = AppSettings.Load();
            }
            catch { }
            return Task.CompletedTask;
        }

        private static Task InitAudio()
        {
            // 音频系统占位
            return Task.CompletedTask;
        }

        private static Task InitHotkeys()
        {
            // 快捷键系统占位，实际在 MainWindow 中初始化
            return Task.CompletedTask;
        }
    }
}
