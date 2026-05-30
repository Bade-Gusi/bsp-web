using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BeiShuiCS2.Services;

namespace BeiShuiCS2
{
    public static class DialogHelper
    {
        /// <summary>
        /// 显示平台自定义的反作弊封锁提示窗口（替代原生 MessageBox）
        /// </summary>
        public static void ShowAntiCheatBlocked(Window? owner = null)
        {
            var dialog = new AntiCheatBlockedWindow();
            if (owner != null)
                dialog.Owner = owner;
            dialog.ShowDialog();
        }
    }


    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;

        public static UserInfo? CurrentUser { get; set; }
        public static bool AntiCheatPassed { get; set; } = true;
        public static bool AntiCheatBlocked { get; set; } = false;
        public static DateTime LastAntiCheatScan { get; set; } = DateTime.Now;
        public static string? CS2Path { get; set; }
        public static string? SavedAuthToken { get; set; }

        // ---- 服务器通讯相关 ----
        /// <summary>当前连接的 CS2 服务器 IP（用于 BAC UDP 心跳）</summary>
        public static string? ServerIP { get; set; }
        /// <summary>当前连接的 CS2 游戏端口</summary>
        public static int ServerGamePort { get; set; } = 27015;

        /// <summary>反作弊客户端通讯模块（UDP 握手 + 加密心跳）</summary>
        public static AntiCheatClient? AntiCheatClient { get; private set; }

        // === 新服务实例（对齐文档方案）===
        public static AuthService Auth { get; private set; } = new();
        public static SignalRService SignalR { get; private set; } = new();
        public static GameLauncherService GameLauncher { get; private set; } = new();
        public static VoiceService Voice { get; private set; } = new();
        public static ApiService Api { get; private set; } = new ApiService("http://127.0.0.1:5000");
        public static AntiCheatClient AntiCheatClientInstance { get; private set; } = new();

        /// <summary>
        /// 确保每个窗口类型只有一个实例。已存在则激活而非新建。
        /// </summary>
        public static T? EnsureSingleWindow<T>() where T : Window
        {
            foreach (Window w in Current.Windows)
            {
                if (w is T existing)
                {
                    if (w.WindowState == WindowState.Minimized)
                        w.WindowState = WindowState.Normal;
                    w.Activate();
                    return existing;
                }
            }
            return null;
        }

        /// <summary>
        /// 全局更新服务器地址。保存设置后调用此方法，所有服务都切换到新地址。
        /// </summary>
        public static void ApplyServerConfig(string host, int port, bool useHttps)
        {
            var scheme = useHttps ? "https" : "http";
            var formattedHost = Services.NetworkHelper.FormatHost(host);
            var baseUrl = $"{scheme}://{formattedHost}:{port}";

            // 更新 ApiService 实例
            Api = new Services.ApiService(baseUrl);

            // 更新静态 ApiClient
            ApiClient.SetBaseUrl(baseUrl);
            if (SavedAuthToken != null)
                ApiClient.SetToken(SavedAuthToken);

            System.Diagnostics.Debug.WriteLine($"[App] 服务器地址已更新: {baseUrl}");
        }

        private DispatcherTimer? _antiCheatHeartbeat;
        private DispatcherTimer? _cs2MonitorTimer;

        /// <summary>
        /// 全局 Window Loaded 事件处理（适配屏幕 + 入场动画 + DWM Acrylic）
        /// 通过隐式 Window Style EventSetter 绑定
        /// </summary>
        private void OnAuroraWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                ScreenAdapter.AdaptWindow(window);

                // 应用毛玻璃效果（AllowsTransparency 窗口使用模拟玻璃，其他使用 DWM Acrylic）
                AcrylicEffectHelper.EnableAcrylic(window);

                // 入场动画：简单透明度淡入（无缩放，避免 DWM 渲染问题）
                window.Opacity = 0;

                // 透明度 0→1
                window.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

                // 启动极光高光线呼吸动画（如果模板中存在该命名元素）
                try
                {
                    var template = window.Template;
                    if (template?.FindName("PART_AuroraLine", window) is FrameworkElement auroraLine)
                    {
                        auroraLine.BeginAnimation(UIElement.OpacityProperty,
                            new DoubleAnimation(0.15, 0.45, TimeSpan.FromSeconds(3))
                            {
                                AutoReverse = true,
                                RepeatBehavior = RepeatBehavior.Forever,
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                            });
                    }
                }
                catch { /* 高光线动画静默失败 */ }

                // 创建浮动粒子（如果模板中存在 PART_ParticleCanvas）
                try
                {
                    var template = window.Template;
                    if (template?.FindName("PART_ParticleCanvas", window) is Canvas particleCanvas)
                    {
                        // 延迟创建粒子，等布局完成后再获取实际尺寸
                        window.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            double w = particleCanvas.ActualWidth > 10 ? particleCanvas.ActualWidth : 400;
                            double h = particleCanvas.ActualHeight > 10 ? particleCanvas.ActualHeight : 400;
                            AnimationHelper.CreateFloatingParticles(particleCanvas,
                                count: 4, minSize: 2, maxSize: 4,
                                durationMin: 8, durationMax: 16);
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
                catch { /* 粒子系统静默失败 */ }
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 全局异常处理边界
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                var logger = Services.ServiceLocator.TryGet<Services.ILogger>();
                logger?.Error($"未捕获异常 (IsTerminating={args.IsTerminating})", ex);
                try
                {
                    var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    Directory.CreateDirectory(logDir);
                    var crashFile = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMddHHmmss}.log");
                    File.WriteAllText(crashFile,
                        $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
                        $"Type: {ex?.GetType().FullName}\r\nMessage: {ex?.Message}\r\nStack:\r\n{ex?.StackTrace}");
                }
                catch { }
            };
            Current.DispatcherUnhandledException += (s, args) =>
            {
                var logger = Services.ServiceLocator.TryGet<Services.ILogger>();
                logger?.Error($"UI线程未处理异常", args.Exception);
                args.Handled = true;
            };

            // 注册服务到 ServiceLocator
            var fileLogger = new Services.FileLogger();
            Services.ServiceLocator.Register<Services.ILogger>(fileLogger);

            fileLogger.Info("应用启动中...");

            // 单实例互斥锁：防止用户手快多次打开平台
            const string mutexName = "BeiShuiCS2_Platform_Instance";
            _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);
            if (!createdNew)
            {
                // 找到已有窗口并激活它
                var existing = Current.Windows.Cast<Window>().FirstOrDefault();
                if (existing != null)
                {
                    if (existing.WindowState == WindowState.Minimized)
                        existing.WindowState = WindowState.Normal;
                    existing.Activate();
                    existing.Topmost = true;
                    existing.Topmost = false;
                }
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }

            // 初始化反作弊通讯客户端
            AntiCheatClient = new AntiCheatClient();
            AntiCheatClient.OnConnectionChanged += OnServerConnectionChanged;

            // 初始化反篡改子系统（程序集完整性、看门狗、时序检测等）
            AntiCheat.InitAntitamper();

            // 后台预加载子系统（不阻塞 UI 启动流程）
            _ = Preloader.InitializeAsync();

            // 从保存的设置加载服务器地址
            var settings = AppSettings.Load();

            // 设置默认 API 地址备用（同时更新 ApiService 和静态 ApiClient）
            if (!string.IsNullOrEmpty(settings.ServerUrl) && settings.ServerPort > 0)
            {
                var baseUrl = Services.NetworkHelper.BuildApiBaseUrl();
                Api = new ApiService(baseUrl);
                ApiClient.SetBaseUrl(baseUrl);
                ApiClient.SetToken(App.SavedAuthToken);
            }
            else
            {
                Api = new ApiService("http://127.0.0.1:5000");
                ApiClient.SetBaseUrl("http://127.0.0.1:5000");
            }

            // 尝试加载保存的 Token
            var savedToken = Auth.LoadToken();
            if (savedToken != null)
            {
                SavedAuthToken = savedToken;
                ApiClient.SetToken(savedToken);
            }

            // IPv6 检查（不阻塞启动）
            if (!settings.IPv6Checked && settings.IPv6AutoEnable)
                CheckIPv6OnStartup(settings);

            // 首次启动 → 欢迎窗口；否则 → 登录窗口
            // 欢迎窗口中点击"开始"会弹出服务器配置窗口，配置完才到登录
            if (settings.IsFirstLaunch)
            {
                var firstLaunch = new FirstLaunchWindow();
                firstLaunch.Show();
            }
            else
            {
                var login = new LoginWindow();
                login.Show();
            }

            // CS2 路径不在此处自动检测 — 用户可在设置页面手动指定
            CS2Path = null;

            // 启动反作弊心跳
            StartAntiCheatHeartbeat();

            // 启动 CS2 进程监控（自动连接/断开 BAC 服务器）
            StartCS2Monitor();
        }

        /// <summary>
        /// 连接到指定的 CS2 游戏服务器，自动启动 BAC 心跳
        /// </summary>
        public static void ConnectToGameServer(string ip, int port)
        {
            ServerIP = ip;
            ServerGamePort = port;
            AntiCheatClient?.ConnectToServer(ip, port);
            System.Diagnostics.Debug.WriteLine($"[App] 连接游戏服务器: {ip}:{port}，BAC 心跳已启动");
        }

        /// <summary>
        /// 断开游戏服务器连接
        /// </summary>
        public static void DisconnectFromGameServer()
        {
            ServerIP = null;
            AntiCheatClient?.Disconnect();
            System.Diagnostics.Debug.WriteLine("[App] 断开游戏服务器，BAC 心跳已停止");
        }

        private static void OnServerConnectionChanged(bool connected)
        {
            System.Diagnostics.Debug.WriteLine($"[App] BAC 服务器连接状态: {(connected ? "已连接" : "已断开")}");
        }

        /// <summary>
        /// 启动时检查 IPv6 状态，如果未启用则提示启用
        /// </summary>
        private static void CheckIPv6OnStartup(AppSettings settings)
        {
            try
            {
                // 检查服务器域名是否能解析 IPv6
                var host = settings.ServerUrl;
                bool hostSupportsIPv6 = false;
                try
                {
                    var addrs = System.Net.Dns.GetHostAddresses(host);
                    hostSupportsIPv6 = addrs.Any(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                }
                catch { }

                // 如果服务器支持 IPv6 但本机未启用，提示
                if (hostSupportsIPv6 && !Services.IPv6Helper.IsIPv6Enabled())
                {
                    var result = System.Windows.MessageBox.Show(
                        $"服务器 {host} 支持 IPv6 连接，但您本机的 IPv6 似乎未启用。\n\n" +
                        "是否启用 IPv6？启用后可获得更稳定的连接。\n\n" +
                        "（需要管理员权限，仅询问一次）",
                        "IPv6 推荐启用",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        Services.IPv6Helper.EnableIPv6();
                    }
                }

                // 标记已检查过
                settings.IPv6Checked = true;
                settings.Save();
            }
            catch { /* 静默失败，不阻塞启动 */ }
        }

        /// <summary>
        /// CS2 进程监控：自动检测 CS2 启动/关闭，管理 BAC 连接
        /// </summary>
        private void StartCS2Monitor()
        {
            _cs2MonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _cs2MonitorTimer.Tick += (s, e) =>
            {
                bool cs2Running = System.Diagnostics.Process.GetProcessesByName("cs2").Length > 0;

                if (cs2Running && !string.IsNullOrEmpty(ServerIP))
                {
                    // CS2 在运行且我们有目标服务器，确保 BAC 客户端已连接
                    if (AntiCheatClient != null && !AntiCheatClient.IsConnected)
                    {
                        AntiCheatClient.ConnectToServer(ServerIP, ServerGamePort);
                    }
                }

                // 检测反作弊完整性 — 如果 cs2 运行但 BAC 客户端断开太久，强制关闭 CS2
                if (cs2Running && AntiCheatBlocked)
                {
                    KillCS2Process();
                }
            };
            _cs2MonitorTimer.Start();
        }

        private void StartAntiCheatHeartbeat()
        {
            _antiCheatHeartbeat = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15) // 每15秒检查一次
            };
            _antiCheatHeartbeat.Tick += (s, e) =>
            {
                if (CurrentUser != null)
                {
                    try
                    {
                        var result = AntiCheat.PerformFullCheckWithReason();
                        AntiCheatPassed = result.Ok;
                        LastAntiCheatScan = DateTime.Now;

                        if (!result.Ok)
                        {
                            // 反作弊检查失败 - 永久封锁游戏入口
                            AntiCheatBlocked = true;
                            LogAntiCheatFailure(result.Reason);

                            // 关闭 CS2 进程
                            KillCS2Process();

                            // 断开 BAC 服务器连接
                            DisconnectFromGameServer();

                            // 弹出警告窗口提醒用户（UI线程）
                            Current.Dispatcher.Invoke(() =>
                            {
                                var warning = new AntiCheatWarningWindow();
                                warning.Owner = Current.MainWindow;
                                warning.ShowDialog();
                            });

                            // 弹出全局违规窗口（UI线程）
                            Current.Dispatcher.Invoke(() =>
                            {
                                var alert = new AntiCheatAlertWindow(
                                    $"违规类型: {result.Type}",
                                    $"检测详情: {result.Reason}\n\n为保护账号安全，CS2 已被自动关闭。\n\n" +
                                    $"如果您正在正常运行本平台，这可能是误报。\n" +
                                    $"请关闭所有可疑程序后重启平台重试。\n" +
                                    $"如问题持续，请联系客服并提供下方的错误码。",
                                    $"BAC-{DateTime.Now:yyyyMMddHHmmss}"
                                );
                                alert.Owner = Current.MainWindow;
                                alert.ShowDialog();
                            });

                            // 强制关闭所有窗口（防止用户在违规后继续操作或卡时间）
                            Current.Dispatcher.Invoke(() =>
                            {
                                try { Current.Shutdown(); }
                                catch { Environment.Exit(1); }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AntiCheat heartbeat error: {ex.Message}");
                    }
                }
            };
            _antiCheatHeartbeat.Start();
        }

        private static void KillCS2Process()
        {
            try
            {
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("cs2"))
                {
                    proc.Kill();
                    proc.WaitForExit(5000);
                }
            }
            catch { }
        }

        /// <summary>
        /// 异步检测节日和生日
        /// </summary>
        private static async System.Threading.Tasks.Task CheckHolidayAndBirthdayAsync(AppSettings settings)
        {
            try
            {
                // 延迟到 UI 完全启动后
                await System.Threading.Tasks.Task.Delay(2000);

                // 节日检测
                var holiday = await Services.HolidayService.GetTodayHolidayAsync();
                if (holiday != null)
                {
                    var holidayKey = $"{DateTime.Now:yyyy-MM-dd}_{holiday.Name}";
                    if (settings.LastDismissedHoliday != holidayKey)
                    {
                        await Current.Dispatcher.InvokeAsync(() =>
                        {
                            var popup = new HolidayPopupWindow(holiday);
                            popup.ShowDialog();
                        });
                    }
                }

                // 生日检测（仅当用户已登录且有 Token 时）
                if (CurrentUser != null && !string.IsNullOrEmpty(SavedAuthToken))
                {
                    var birthdayResult = await Services.HolidayService.CheckBirthdayAsync(0);
                    // 通过 profile 获取 userId
                    var profileResult = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/auth/profile");
                    if (profileResult.Success)
                    {
                        var userId = profileResult.Data.TryGetProperty("id", out var id) ? id.GetInt64() : 0L;
                        if (userId > 0)
                        {
                            birthdayResult = await Services.HolidayService.CheckBirthdayAsync(userId);
                            if (birthdayResult?.IsBirthday == true)
                            {
                                var birthdayKey = $"{DateTime.Now:yyyy-MM-dd}_生日";
                                if (settings.LastDismissedHoliday != birthdayKey)
                                {
                                    await Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        var popup = new HolidayPopupWindow(new Services.HolidayInfo
                                        {
                                            Name = "生日",
                                            Date = DateTime.Now.ToString("MM-dd"),
                                            Message = birthdayResult.Message,
                                            Color1 = "#FF69B4",
                                            Color2 = "#FFD700",
                                            Emoji = "🎂"
                                        });
                                        popup.ShowDialog();
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void LogAntiCheatFailure(string reason)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BeiShuiCS2", "Logs");
                Directory.CreateDirectory(logPath);

                string logFile = Path.Combine(logPath, $"anticheat_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] AntiCheat failed: {reason}\n");
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _antiCheatHeartbeat?.Stop();
            _cs2MonitorTimer?.Stop();
            AntiCheatClient?.Dispose();
            GameLauncher.Dispose();
            base.OnExit(e);
        }
    }

    public class UserInfo
    {
        public string Username { get; set; } = "";
        public string SteamId { get; set; } = "";
        public string Avatar { get; set; } = "";
        public string Token { get; set; } = "";
        public int Elo { get; set; } = 1000;
    }

    /// <summary>
    /// 匹配后端 /api/auth/login 返回格式:
    /// { "token": "...", "user": { "id": 1, "username": "...", "nickname": "...", "mmr": 1000, "rankName": "..." } }
    /// </summary>
    public class AuthResponse
    {
        public string? Token { get; set; }
        public AuthUser? User { get; set; }
    }

    public class AuthUser
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? Nickname { get; set; }
        public int Mmr { get; set; }
        public string? RankName { get; set; }
    }
}
