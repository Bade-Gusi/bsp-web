using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;


namespace BeiShuiCS2
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Friend> Friends { get; set; } = new();
        public ObservableCollection<MatchResult> RecentMatches { get; set; } = new();

        private readonly DateTime _startupTime = DateTime.Now;
        private PerformanceCounter? _cpuCounter;
        private DispatcherTimer? _systemTimer;
        private NotifyIconManager? _trayIcon;
        private HotkeyManager? _hotkeys;
        private WebView2? _animationBg;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
            this.Topmost = true;
            this.Loaded += (s, e) => this.Topmost = false;

            // 订阅语言切换事件，刷新导航按钮文本
            LanguageManager.LanguageChanged += RefreshNavigationText;

            // CPU 计数器延迟初始化（避免构造函数阻塞UI线程导致登录卡死）
            _cpuCounter = null;
        }

        private void InitCpuCounter()
        {
            if (_cpuCounter != null) return;
            try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { _cpuCounter = null; }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口入场动画
            AnimationHelper.WindowEntrance(this);

            // 初始化 WebView2 动画背景（异步）
            _ = InitAnimationBackgroundAsync();

            // 反作弊指示器呼吸脉冲
            AnimationHelper.Pulse(antiCheatStatus, 0.5, 1, 1.5);

            // 内容区背景缓慢呼吸变色
            AnimationHelper.AnimateBackgroundGradient(contentArea,
                Color.FromRgb(0x0F, 0x1A, 0x12),    // #0F1A12 深绿
                Color.FromRgb(0x0A, 0x15, 0x0D),    // #0A150D 更深的绿
                8.0);

            // 启动增强加载动画
            ShowLoading("正在加载平台数据...", "");

            // 使用 Preloader 异步预加载所有子系统
            var progress = new Progress<string>(step =>
            {
                Dispatcher.Invoke(() => txtLoadingSub.Text = step);
            });

            await Preloader.InitializeAsync(progress);

            // 加载平台数据
            await Dispatcher.InvokeAsync(() =>
            {
                // 异步加载不阻塞 UI，不等待完成
                _ = LoadDataAsync();
                StartSystemMonitor();
            }, System.Windows.Threading.DispatcherPriority.Background);

            // 完成加载
            await Dispatcher.InvokeAsync(() =>
            {
                HideLoading();
                SetupAnimations();
                ApplyDisplaySettings();
                InitTrayIcon();
                InitHotkeys();
                CheckForUpdatesAsync();
                CheckHolidayAndBirthdayAsync();
                ShowLoginNotification();
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// 初始化 WebView2 JS 动画引擎背景
        /// </summary>
        private async Task InitAnimationBackgroundAsync()
        {
            try
            {
                // 只附加到内容区 Grid（不包含侧边栏），防止 WebView2 HWND/DCOMP 层拦截侧边栏点击
                _animationBg = await WebView2AnimationHelper.AttachToGrid(contentArea);
                // 性能调优：中端配置
                await WebView2AnimationHelper.SetParticleCount(_animationBg, 20);
                await WebView2AnimationHelper.SetSpeed(_animationBg, 0.5);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnimBg] Init error: {ex.Message}");
            }
        }

        private void StartSystemMonitor()
        {
            _systemTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _systemTimer.Tick += (s, e) => UpdateSystemStatus();
            _systemTimer.Start();
            UpdateSystemStatus(); // 立即更新一次
        }

        private void UpdateSystemStatus()
        {
            // 运行时间
            var elapsed = DateTime.Now - _startupTime;
            txtUptime.Text = elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
                : $"{elapsed.Minutes}m {elapsed.Seconds}s";

            // CS2 进程检测
            var cs2Procs = Process.GetProcessesByName("cs2");
            txtCs2Status.Text = cs2Procs.Length > 0 ? "运行中" : "未运行";
            txtCs2Status.Foreground = cs2Procs.Length > 0
                ? (Brush)FindResource("PrimaryBrush")
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

            // CPU 使用率（延迟初始化）
            if (_cpuCounter == null) InitCpuCounter();
            if (_cpuCounter != null)
            {
                try
                {
                    float cpu = _cpuCounter.NextValue();
                    txtCpuStatus.Text = $"CPU: {cpu:F1}%";
                }
                catch { txtCpuStatus.Text = ""; }
            }

            // 内存使用
            try
            {
                using var proc = Process.GetCurrentProcess();
                long memMB = proc.WorkingSet64 / (1024 * 1024);
                txtMemStatus.Text = $"内存: {memMB} MB";
            }
            catch { txtMemStatus.Text = ""; }

            // 反作弊状态
            if (App.AntiCheatBlocked)
            {
                txtAntiCheatInfo.Text = "已封锁 ⛔";
                txtAntiCheatInfo.Foreground = (Brush)FindResource("DangerBrush");
            }
            else
            {
                txtAntiCheatInfo.Text = App.AntiCheatPassed ? "运行正常" : "异常";
                txtAntiCheatInfo.Foreground = App.AntiCheatPassed
                    ? (Brush)FindResource("PrimaryBrush")
                    : (Brush)FindResource("DangerBrush");
            }

            // 反作弊面板显示 + 全局封锁横幅
            if (App.AntiCheatBlocked)
            {
                txtAntiCheatStatusDisplay.Text = "已封锁 ⛔";
                txtAntiCheatStatusDisplay.Foreground = (Brush)FindResource("DangerBrush");
                antiCheatStatus.Fill = (Brush)FindResource("DangerBrush");
                txtAntiCheatLastScan.Text = "游戏入口已被封锁";
                txtAntiCheatLastScan.Foreground = (Brush)FindResource("DangerBrush");
                antiCheatStatus.Width = 10;
                antiCheatStatus.Height = 10;

                // 显示全局封锁横幅
                if (banBlockedBanner != null) banBlockedBanner.Visibility = Visibility.Visible;
            }
            else
            {
                txtAntiCheatStatusDisplay.Text = App.AntiCheatPassed ? "运行正常" : "检测异常";
                txtAntiCheatStatusDisplay.Foreground = App.AntiCheatPassed
                    ? (Brush)FindResource("PrimaryBrush")
                    : (Brush)FindResource("DangerBrush");
                var scanElapsed = DateTime.Now - App.LastAntiCheatScan;
                txtAntiCheatLastScan.Text = $"上次扫描: {scanElapsed.TotalMinutes:F0}分{scanElapsed.Seconds}秒前";
                txtAntiCheatLastScan.Foreground = (Brush)FindResource("TextMutedBrush");
                antiCheatStatus.Fill = App.AntiCheatPassed
                    ? (Brush)FindResource("PrimaryBrush")
                    : (Brush)FindResource("DangerBrush");
                antiCheatStatus.Width = 8;
                antiCheatStatus.Height = 8;

                // 隐藏全局封锁横幅
                if (banBlockedBanner != null) banBlockedBanner.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.GetPosition(this).Y < 64)
                this.DragMove();
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            // 更新用户名显示
            if (App.CurrentUser != null)
            {
                txtUsername.Text = App.CurrentUser.Username;
                txtUserInitial.Text = App.CurrentUser.Username.Substring(0, 1).ToUpper();

                // 尝试加载 Steam 头像，失败则显示默认头像
                if (!string.IsNullOrEmpty(App.CurrentUser.SteamId))
                {
                    try
                    {
                        var avatar = SteamHelper.GetAvatar(App.CurrentUser.SteamId);
                        if (avatar != null)
                        {
                            imgUserAvatar.Source = avatar;
                            imgUserAvatar.Clip = new EllipseGeometry { RadiusX = 20, RadiusY = 20, Center = new Point(20, 20) };
                            imgUserAvatar.Visibility = Visibility.Visible;
                            txtUserInitial.Visibility = Visibility.Collapsed;
                            return;
                        }
                    }
                    catch { }
                }

                // Steam 头像不可用时显示默认头像
                try
                {
                    var defaultAvatar = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("pack://application:,,,/default_avatar.png"));
                    imgUserAvatar.Source = defaultAvatar;
                    imgUserAvatar.Clip = new EllipseGeometry { RadiusX = 20, RadiusY = 20, Center = new Point(20, 20) };
                    imgUserAvatar.Visibility = Visibility.Visible;
                    txtUserInitial.Visibility = Visibility.Collapsed;
                }
                catch { }
            }

            try
            {
                // 从 API 加载战绩
                var matchResult = await ApiClient.GetAsync<List<MatchHistoryItem>>("/api/matches");
                if (matchResult.Success && matchResult.Data != null)
                {
                    RecentMatches.Clear();
                    foreach (var m in matchResult.Data)
                    {
                        RecentMatches.Add(new MatchResult
                        {
                            Map = m.Map ?? "未知",
                            Date = m.PlayedAt,
                            Score = m.Score ?? "",
                            ResultColor = new SolidColorBrush(m.Result == "胜" ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0xF4, 0x3F, 0x5E))
                        });
                    }
                }

                // 从 API 加载好友列表
                var friendResult = await ApiClient.GetAsync<List<FriendItem>>("/api/friends");
                if (friendResult.Success && friendResult.Data != null)
                {
                    Friends.Clear();
                    foreach (var f in friendResult.Data)
                    {
                        Friends.Add(new Friend
                        {
                            Name = f.FriendName,
                            IsOnline = f.IsOnline,
                            SteamID = null
                        });
                    }
                }
            }
            catch { /* 静默失败，保留空数据 */ }

            lbRecentMatches.ItemsSource = RecentMatches;
        }

        private async void LoadMockData()
        {
            await LoadDataAsync();
        }

        private void RecentMatch_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is MatchResult match)
            {
                var detail = new MatchDetailWindow { Owner = this };
                detail.ShowDialog();
            }
        }

        private void SetupAnimations()
        {
            // === anime.js 风格：逐行交错飞入 ===

            // 统计卡片从左到右交错飞入
            var statsPanel = contentGrid.Children.OfType<StackPanel>().FirstOrDefault();
            if (statsPanel != null)
            {
                AnimationHelper.AnimeStagger(statsPanel.Children, "up", 30, 0.2, 0.1);
            }

            // 主要内容区域
            var mainGrid = contentGrid.Children.OfType<Grid>().Skip(1).FirstOrDefault();
            if (mainGrid != null)
            {
                // 左侧操作区域（从下滑入，弹性交错）
                var leftStack = mainGrid.Children.OfType<StackPanel>().FirstOrDefault();
                if (leftStack != null)
                {
                    AnimationHelper.AnimeStagger(leftStack.Children, "up", 25, 0.5, 0.12);
                }

                // 右侧区域（从右滑入 + 弹性缩放）
                var rightStack = mainGrid.Children.OfType<StackPanel>().Skip(1).FirstOrDefault();
                if (rightStack != null)
                {
                    int idx = 0;
                    foreach (UIElement child in rightStack.Children)
                    {
                        if (child is Border b && b.IsVisible)
                        {
                            // anime.js 风格：同时 slide + bounce
                            b.Opacity = 0;
                            b.RenderTransformOrigin = new Point(0.5, 0.5);
                            var tt = new TranslateTransform(40, 0);
                            b.RenderTransform = tt;

                            double delay = 0.6 + 0.12 * idx;
                            b.BeginAnimation(OpacityProperty,
                                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                                { BeginTime = TimeSpan.FromSeconds(delay), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                            tt.BeginAnimation(TranslateTransform.XProperty,
                                new DoubleAnimation(40, 0, TimeSpan.FromSeconds(0.45))
                                { BeginTime = TimeSpan.FromSeconds(delay), EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });

                            // 卡片内元素呼吸辉光
                            var glowBorder = b;
                            AnimationHelper.AttachGlowPulse(glowBorder, Color.FromRgb(0x4A, 0xDE, 0x80), 0.02, 0.06, 4);
                            idx++;
                        }
                    }
                }
            }

            // 页脚从下滑入（最后出现）
            var footer = contentGrid.Children.OfType<Border>().LastOrDefault();
            if (footer != null)
                AnimationHelper.SlideIn(footer, 0, 25, 0.5, 1.2);

            // 各导航按钮交错淡入（左侧导航）- anime.js 波浪式
            var navPanel = FindChild<StackPanel>(this);
            if (navPanel != null)
            {
                int idx = 0;
                foreach (UIElement child in navPanel.Children)
                {
                    if (child is Button navBtn && navBtn.IsVisible && navBtn.Style == (Style)FindResource("NavItemStyle"))
                    {
                        navBtn.Opacity = 0;
                        double delay = 0.3 + 0.04 * idx;
                        navBtn.BeginAnimation(OpacityProperty,
                            new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                            {
                                BeginTime = TimeSpan.FromSeconds(delay),
                                EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 6, EasingMode = EasingMode.EaseOut }
                            });
                        idx++;
                    }
                }
            }

            // 页脚文字漂浮动画
            if (footer != null)
            {
                var children = new[] { txtCpuStatus, txtMemStatus };
                int idx2 = 0;
                foreach (var tb in children)
                {
                    if (tb != null && !string.IsNullOrEmpty(tb.Text))
                        AnimationHelper.FloatingAnimation(tb, 2, 3 + idx2 * 0.3, 1.3 + idx2 * 0.1);
                    idx2++;
                }
            }

            // 对所有 Card3DHover 卡片附加 3D 倾斜效果
            Attach3DTiltToAllCards();
        }

        /// <summary>
        /// 遍历内容区所有使用 Card3DHover 样式的 Border，附加 3D 鼠标跟随倾斜效果
        /// </summary>
        private void Attach3DTiltToAllCards()
        {
            var tiltStyle = FindResource("Card3DHover") as Style;
            if (tiltStyle == null) return;

            var cards = FindVisualChildren<Border>(contentGrid);
            foreach (var card in cards)
            {
                if (card.Style == tiltStyle)
                {
                    try
                    {
                        AnimationHelper.Attach3DTilt(card, maxAngle: 4);
                    }
                    catch { /* 3D tilt 静默失败 */ }
                }
            }
        }

        /// <summary>
        /// 语言切换时刷新导航栏所有按钮文本
        /// </summary>
        private void RefreshNavigationText()
        {
            // 导航按钮文本通过遍历 navItems 更新
            // 因为导航按钮在 StackPanel 中，通过查找子元素更新
            Dispatcher.Invoke(() =>
            {
                // 由于 XAML 导航按钮硬编码，我们直接更新能找到的命名元素
                // 这里用 Name 查找的方式不够可靠，改用遍历 nav 容器
                var navPanel = FindChild<StackPanel>(this);
                if (navPanel == null) return;

                var navMap = new Dictionary<string, string>
                {
                    ["首页"] = "Home",
                    ["快速匹配"] = "QuickMatch",
                    ["1v1 对战"] = "Duel",
                    ["开服务器"] = "CreateServer",
                    ["房间大厅"] = "RoomHall",
                    ["服务器"] = "Server",
                    ["好友"] = "Friends",
                    ["聊天"] = "Chat",
                    ["战绩"] = "History",
                    ["Demo回放"] = "Demo",
                    ["排行榜"] = "Leaderboard",
                    ["成就"] = "Achievements",
                    ["皮肤市场"] = "SkinMarket",
                    ["匹配队列"] = "MatchingQueue",
                    ["小游戏"] = "MiniGames",
                    ["语音通话"] = "VoiceCall",
                    ["屏幕分享"] = "ScreenShare",
                    ["设置"] = "Settings",
                };

                foreach (var child in navPanel.Children)
                {
                    if (child is System.Windows.Controls.Button btn)
                    {
                        var innerStack = btn.Content as System.Windows.Controls.StackPanel;
                        if (innerStack == null) continue;
                        foreach (var innerChild in innerStack.Children)
                        {
                            if (innerChild is System.Windows.Controls.TextBlock tb && navMap.ContainsKey(tb.Text))
                            {
                                tb.Text = LanguageManager.GetString(navMap[tb.Text]);
                                break;
                            }
                        }
                    }
                }
            });
        }

        // 导航事件
        private void NavHome_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "首页";

            // 每次回到首页时从 API 刷新数据
            _ = LoadDataAsync();

            // 内容过渡动画
            AnimateContentTransition();
            var scrollViewer = FindChild<ScrollViewer>(contentGrid);
            scrollViewer?.ScrollToTop();
        }

        /// <summary>
        /// 页面内容切换时的淡入淡出过渡（anime.js 风格弹性过渡）
        /// </summary>
        private void AnimateContentTransition()
        {
            if (contentGrid == null) return;

            // Phase 1: 快速缩放淡出 + 轻微上移
            contentGrid.RenderTransformOrigin = new Point(0.5, 0.5);
            contentGrid.RenderTransform = new ScaleTransform(1, 1);
            var scaleDownX = new DoubleAnimation(1, 0.92, TimeSpan.FromSeconds(0.08));
            var scaleDownY = new DoubleAnimation(1, 0.92, TimeSpan.FromSeconds(0.08));
            var fadeOut = new DoubleAnimation(1, 0.4, TimeSpan.FromSeconds(0.08));
            fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            contentGrid.BeginAnimation(OpacityProperty, fadeOut);
            contentGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDownX);
            contentGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDownY);

            // Phase 2: 弹性弹回 + 淡入
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.12) };
            t.Tick += (_, _) =>
            {
                t.Stop();
                contentGrid.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0.4, 1, TimeSpan.FromSeconds(0.35))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
                contentGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(0.4))
                    {
                        EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
                    });
                contentGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(0.4))
                    {
                        EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
                    });
            };
            t.Start();
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var descendant = FindChild<T>(child);
                if (descendant != null) return descendant;
            }
            return null;
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild) yield return tChild;
                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        private void NavMatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            if (App.AntiCheatBlocked)
            {
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }
            var quickMatch = new QuickMatchWindow { Owner = this };
            quickMatch.ShowDialog();
        }

        private void NavDuel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            var duel = new DuelWindow { Owner = this };
            duel.ShowDialog();
        }

        private void NavCreateServer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            if (App.AntiCheatBlocked)
            {
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }
            var createServer = new CreateServerWindow { Owner = this };
            createServer.ShowDialog();
        }

        private void NavRoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            if (App.AntiCheatBlocked)
            {
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }
            var joinRoom = new JoinRoomWindow { Owner = this };
            joinRoom.ShowDialog();
        }

        private void NavServer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            if (App.AntiCheatBlocked)
            {
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }
            var serverManager = new ServerManagerWindow { Owner = this };
            serverManager.ShowDialog();
        }

        private void NavChat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            var chat = new ChatWindow { Owner = this };
            chat.ShowDialog();
        }

        private async void NavFriends_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "好友";

            // 打开好友页面前先刷新好友数据
            var friendResult = await ApiClient.GetAsync<List<FriendItem>>("/api/friends");
            if (friendResult.Success && friendResult.Data != null)
            {
                Friends.Clear();
                foreach (var f in friendResult.Data)
                {
                    Friends.Add(new Friend { Name = f.FriendName, IsOnline = f.IsOnline });
                }
            }

            // 打开好友管理窗口
            var friendsWindow = new Window
            {
                Title = "好友列表",
                Width = 500,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A1A10")),
                FontFamily = new FontFamily("Segoe UI")
            };

            var grid = new Grid { Margin = new Thickness(24) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 标题
            var titleBlock = new TextBlock
            {
                Text = "好友列表",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(titleBlock, 0);
            grid.Children.Add(titleBlock);

            // 在线好友
            var onlineHeader = new TextBlock
            {
                Text = $"在线好友 ({Friends.Count(f => f.IsOnline)})",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(onlineHeader, 1);
            grid.Children.Add(onlineHeader);

            var onlineList = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ItemsSource = Friends.Where(f => f.IsOnline).ToList(),
                Margin = new Thickness(0, 0, 0, 16)
            };
            onlineList.ItemTemplate = CreateFriendItemTemplate();
            Grid.SetRow(onlineList, 2);
            grid.Children.Add(onlineList);

            // 离线好友
            var offlineHeader = new TextBlock
            {
                Text = $"离线好友 ({Friends.Count(f => !f.IsOnline)})",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(offlineHeader, 3);
            grid.Children.Add(offlineHeader);

            var offlineList = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ItemsSource = Friends.Where(f => !f.IsOnline).ToList(),
                Margin = new Thickness(0, 0, 0, 16)
            };
            offlineList.ItemTemplate = CreateFriendItemTemplate();
            Grid.SetRow(offlineList, 4);
            grid.Children.Add(offlineList);

            // 添加好友按钮
            var addBtn = new Button
            {
                Content = "+ 添加好友",
                Style = (Style)FindResource("BtnPrimary"),
                Height = 44,
                Margin = new Thickness(0, 8, 0, 0)
            };
            addBtn.Click += (s, ev) =>
            {
                var addFriend = new AddFriendWindow { Owner = friendsWindow };
                addFriend.ShowDialog();
            };
            Grid.SetRow(addBtn, 5);
            grid.Children.Add(addBtn);

            // 邀请好友按钮
            var inviteBtn = new Button
            {
                Content = "邀请好友对战",
                Style = (Style)FindResource("BtnGhost"),
                Height = 44,
                Margin = new Thickness(0, 8, 0, 0)
            };
            inviteBtn.Click += (s, ev) =>
            {
                var invite = new FriendInviteWindow { Owner = friendsWindow };
                invite.ShowDialog();
            };
            Grid.SetRow(inviteBtn, 6);
            grid.Children.Add(inviteBtn);

            // Row 7: 好友请求
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var requestBtn = new Button
            {
                Content = "📩 好友请求",
                Style = (Style)FindResource("BtnGhost"),
                Height = 44,
                Margin = new Thickness(0, 8, 0, 0)
            };
            requestBtn.Click += (s, ev) =>
            {
                new FriendRequestsWindow { Owner = friendsWindow }.ShowDialog();
            };
            Grid.SetRow(requestBtn, 7);
            grid.Children.Add(requestBtn);

            // Row 9: 私聊选中好友
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var chatBtn = new Button
            {
                Content = "💬 私聊选中好友",
                Style = (Style)FindResource("BtnGhost"),
                Height = 44,
                Margin = new Thickness(0, 8, 0, 0)
            };
            chatBtn.Click += (s, ev) =>
            {
                var selectedFriend = onlineList.SelectedItem as Friend ?? offlineList.SelectedItem as Friend;
                if (selectedFriend == null)
                {
                    MessageBox.Show("请先选择一个好友", "提示");
                    return;
                }
                var privateChat = new PrivateChatWindow(selectedFriend.FriendId, selectedFriend.Name) { Owner = friendsWindow };
                privateChat.ShowDialog();
            };
            Grid.SetRow(chatBtn, 8);
            grid.Children.Add(chatBtn);

            // Row 10: 设置备注 → InputDialogWindow
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var renameBtn = new Button
            {
                Content = "设置备注",
                Style = (Style)FindResource("BtnGhost"),
                Height = 44,
                Margin = new Thickness(0, 8, 0, 0)
            };
            renameBtn.Click += (s, ev) =>
            {
                var selectedFriend = onlineList.SelectedItem as Friend ?? offlineList.SelectedItem as Friend;
                if (selectedFriend == null)
                {
                    MessageBox.Show("请先选择一个好友", "提示");
                    return;
                }
                var input = new InputDialogWindow("修改备注", $"为 {selectedFriend.Name} 设置新备注名：", selectedFriend.Name);
                input.Owner = friendsWindow;
                if (input.ShowDialog() == true && !string.IsNullOrEmpty(input.Result))
                {
                    var oldName = selectedFriend.Name;
                    selectedFriend.Name = input.Result;
                    onlineList.ItemsSource = Friends.Where(f => f.IsOnline).ToList();
                    offlineList.ItemsSource = Friends.Where(f => !f.IsOnline).ToList();
                    var mainWin = Application.Current.MainWindow as MainWindow;
                    mainWin?.ShowToast($"已将 {oldName} 的备注修改为 {input.Result}");
                }
            };
            Grid.SetRow(renameBtn, 10);
            grid.Children.Add(renameBtn);

            // Row 9: 删除好友 → ConfirmDeleteFriendWindow
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var deleteBtn = new Button
            {
                Content = "删除好友",
                Style = (Style)FindResource("BtnDanger"),
                Height = 44,
                Margin = new Thickness(0, 8, 0, 0)
            };
            deleteBtn.Click += (s, ev) =>
            {
                var selectedFriend = onlineList.SelectedItem as Friend ?? offlineList.SelectedItem as Friend;
                if (selectedFriend == null)
                {
                    MessageBox.Show("请先选择一个好友", "提示");
                    return;
                }
                var confirm = new ConfirmDeleteFriendWindow { Owner = friendsWindow };
                confirm.ShowDialog();
                if (confirm.Confirmed && selectedFriend.FriendId > 0)
                {
                    var friendName = selectedFriend.Name;
                    var friendId = selectedFriend.FriendId;

                    // 调 API 删除
                    _ = ApiClient.DeleteAsync($"/api/friends/{friendId}");

                    Friends.Remove(selectedFriend);
                    onlineList.ItemsSource = Friends.Where(f => f.IsOnline).ToList();
                    offlineList.ItemsSource = Friends.Where(f => !f.IsOnline).ToList();
                    onlineHeader.Text = $"在线好友 ({Friends.Count(f => f.IsOnline)})";
                    offlineHeader.Text = $"离线好友 ({Friends.Count(f => !f.IsOnline)})";
                    var mainWin = Application.Current.MainWindow as MainWindow;
                    mainWin?.ShowToast($"已删除好友 {friendName}");
                }
            };
            Grid.SetRow(deleteBtn, 11);
            grid.Children.Add(deleteBtn);

            // 双击好友 → 私聊
            onlineList.PreviewMouseLeftButtonDown += (s, ev) =>
            {
                if (ev.ClickCount == 2 && onlineList.SelectedItem is Friend friend)
                {
                    var privateChat = new PrivateChatWindow { Owner = friendsWindow };
                    privateChat.Title = $"私聊 - {friend.Name}";
                    privateChat.ShowDialog();
                }
            };
            offlineList.PreviewMouseLeftButtonDown += (s, ev) =>
            {
                if (ev.ClickCount == 2 && offlineList.SelectedItem is Friend friend)
                {
                    var privateChat = new PrivateChatWindow { Owner = friendsWindow };
                    privateChat.Title = $"私聊 - {friend.Name}";
                    privateChat.ShowDialog();
                }
            };

            friendsWindow.Content = grid;
            friendsWindow.ShowDialog();
        }

        private DataTemplate CreateFriendItemTemplate()
        {
            var template = new DataTemplate(typeof(Friend));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(12));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 4));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1A")));

            var gridFactory = new FrameworkElementFactory(typeof(Grid));
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            var col3 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col3.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            gridFactory.AppendChild(col1);
            gridFactory.AppendChild(col2);
            gridFactory.AppendChild(col3);

            // 状态指示器
            var ellipseFactory = new FrameworkElementFactory(typeof(Ellipse));
            ellipseFactory.SetValue(Ellipse.WidthProperty, 8.0);
            ellipseFactory.SetValue(Ellipse.HeightProperty, 8.0);
            ellipseFactory.SetValue(Ellipse.FillProperty, new Binding("StatusColor"));
            ellipseFactory.SetValue(Grid.ColumnProperty, 0);
            ellipseFactory.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            gridFactory.AppendChild(ellipseFactory);

            // 名字和状态
            var namePanel = new FrameworkElementFactory(typeof(StackPanel));
            namePanel.SetValue(Grid.ColumnProperty, 1);
            namePanel.SetValue(MarginProperty, new Thickness(8, 0, 8, 0));

            var nameBlock = new FrameworkElementFactory(typeof(TextBlock));
            nameBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameBlock.SetValue(TextBlock.FontSizeProperty, 14.0);
            nameBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")));
            namePanel.AppendChild(nameBlock);

            var statusBlock = new FrameworkElementFactory(typeof(TextBlock));
            statusBlock.SetBinding(TextBlock.TextProperty, new Binding("StatusDisplay"));
            statusBlock.SetValue(TextBlock.FontSizeProperty, 11.0);
            statusBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")));
            namePanel.AppendChild(statusBlock);

            gridFactory.AppendChild(namePanel);

            // ELO
            var eloBlock = new FrameworkElementFactory(typeof(TextBlock));
            eloBlock.SetBinding(TextBlock.TextProperty, new Binding("ELO") { StringFormat = "ELO: {0}" });
            eloBlock.SetValue(Grid.ColumnProperty, 2);
            eloBlock.SetValue(TextBlock.FontSizeProperty, 12.0);
            eloBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")));
            eloBlock.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            gridFactory.AppendChild(eloBlock);

            borderFactory.AppendChild(gridFactory);
            template.VisualTree = borderFactory;
            return template;
        }

        private void NavHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "战绩";
            AnimateContentTransition();
            // 滚动到最近战绩区域
            if (lbRecentMatches != null)
            {
                lbRecentMatches.BringIntoView();
                // 高亮闪烁
                if (lbRecentMatches.Parent is Border parentBorder)
                    AnimationHelper.Flash(parentBorder, Color.FromRgb(0x4A, 0xDE, 0x80), 1);
            }
        }
        private void NavDemo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            var demo = new DemoPlayerWindow { Owner = this };
            demo.ShowDialog();
        }
        private void NavLeaderboard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "排行榜";
            var leaderboard = new LeaderboardWindow { Owner = this };
            leaderboard.ShowDialog();
        }

        private void NavAchievements_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "成就";
            var achievements = new AchievementsWindow { Owner = this };
            achievements.ShowDialog();
        }

        private void NavMarket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "皮肤市场";
            var market = new SkinMarketWindow { Owner = this };
            market.ShowDialog();
        }

        private void NavMatchingQueue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "匹配队列";
            var matching = new MatchingWindow("de_dust2", "CN") { Owner = this };
            matching.ShowDialog();
        }

        private void NavMiniGames_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "小游戏";
            var gameCenter = new GameCenterWindow { Owner = this };
            gameCenter.ShowDialog();
        }

        private void NavWelfare_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            txtPageTitle.Text = "背水公益";
            var welfare = new WelfareWindow { Owner = this };
            welfare.ShowDialog();
        }

        private void NavVoiceCall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            var voiceRoom = new VoiceRoomWindow { Owner = this };
            voiceRoom.ShowDialog();
        }

        private void NavScreenShare_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            var screenShare = new ScreenShareWindow { Owner = this };
            screenShare.ShowDialog();
        }

        private void NavBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            var broadcast = new BroadcastServerWindow { Owner = this };
            broadcast.ShowDialog();
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);
            var settings = new SettingsWindow { Owner = this };
            settings.ShowDialog();
        }

        // 快速操作
        private void QuickMatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);

            if (App.AntiCheatBlocked)
            {
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }
            var quickMatch = new QuickMatchWindow { Owner = this };
            quickMatch.ShowDialog();
        }

        private void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) AnimationHelper.ButtonPressPulse(btn);

            if (App.AntiCheatBlocked)
            {
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }
            var createRoom = new CreateRoomWindow { Owner = this };
            createRoom.ShowDialog();
        }

        private void OpenStats_Click(object sender, RoutedEventArgs e)
        {
            var stats = new StatsOverviewWindow { Owner = this };
            stats.ShowDialog();
        }

        private void OpenGameCenter_Click(object sender, RoutedEventArgs e)
        {
            if (App.AntiCheatBlocked)
            {
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }
            var gameCenter = new GameCenterWindow { Owner = this };
            gameCenter.ShowDialog();
        }

        private void AntiCheatDetail_Click(object sender, RoutedEventArgs e)
        {
            if (App.AntiCheatBlocked)
            {
                // 封锁状态下打开专属封锁详情窗口
                var detail = new BlockedDetailWindow { Owner = this };
                detail.ShowDialog();
            }
            else
            {
                var antiCheat = new AntiCheatStatusWindow { Owner = this };
                antiCheat.ShowDialog();
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要退出登录吗？", "确认", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                AnimationHelper.WindowCloseWithScale(this, () =>
                {
                    var login = new LoginWindow();
                    login.Show();
                });
            }
        }

        // 窗口控制
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button minimizeBtn)
                AnimationHelper.ButtonPressPulse(minimizeBtn);

            // 缩小动画 → 再最小化
            var shrink = new DoubleAnimation(1, 0.85, TimeSpan.FromSeconds(0.12))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var transform = new ScaleTransform(1, 1);
            this.RenderTransform = transform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            shrink.Completed += (_, _) =>
            {
                this.RenderTransform = null;
                var settings = AppSettings.Load();
                if (settings.StartMinimized && _trayIcon != null)
                {
                    this.Hide();
                    ShowToast("背水平台已最小化到系统托盘");
                }
                else
                {
                    this.WindowState = WindowState.Minimized;
                }
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button maximizeBtn)
                AnimationHelper.ButtonPressPulse(maximizeBtn);

            // 放大/缩小动画
            var currentScale = this.WindowState == WindowState.Maximized ? 1.0 : 0.97;
            var targetScale = this.WindowState == WindowState.Maximized ? 0.97 : 1.0;

            var scaleAnim = new DoubleAnimation(currentScale, targetScale, TimeSpan.FromSeconds(0.15))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            var transform = new ScaleTransform(1, 1);
            this.RenderTransform = transform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            scaleAnim.Completed += (_, _) =>
            {
                this.WindowState = this.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                this.RenderTransform = null;
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var settings = AppSettings.Load();

            // 如果启用了最小化到托盘，关闭按钮隐藏到托盘
            if (settings.StartMinimized && _trayIcon != null)
            {
                var result = MessageBox.Show("关闭窗口将最小化到系统托盘，继续运行。\n要完全退出请点击托盘图标 → 退出。\n\n确定要最小化到托盘吗？",
                    "背水对战平台",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    this.Hide();
                    ShowToast("已最小化到系统托盘");
                    return;
                }
                // 选否则继续退出流程
            }

            // 退出确认
            var exitConfirm = new ConfirmExitWindow { Owner = this };
            if (exitConfirm.ShowDialog() != true || !exitConfirm.Confirmed)
                return;

            // 直接杀死进程，确保完全退出
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        // ═══ 自动更新检查 ═══
        private async void CheckForUpdatesAsync()
        {
            try
            {
                var settings = AppSettings.Load();
                if (!settings.AutoUpdate) return;

                var update = await UpdateChecker.CheckForUpdates();
                if (update.IsUpdateAvailable)
                {
                    var result = MessageBox.Show(
                        $"发现新版本 {update.LatestVersion}！\n\n更新内容：\n{update.ReleaseNotes}\n\n是否前往下载？",
                        "发现更新",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (!string.IsNullOrEmpty(update.DownloadUrl))
                            UpdateChecker.OpenDownloadPage(update.DownloadUrl);
                        else
                            UpdateChecker.OpenDownloadPage("https://github.com/BeiShuiStudio/BeiShuiCS2/releases");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateCheck] 当前已是最新版本");
                }
            }
            catch (HttpRequestException ex)
            {
                // 网络不通时的友好提示
                System.Diagnostics.Debug.WriteLine($"[UpdateCheck] 网络错误: {ex.Message}");
                ShowToast("⚠️ 检查更新失败，请检查网络连接");
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[UpdateCheck] 检查超时");
                ShowToast("⚠️ 检查更新超时");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCheck] 未知错误: {ex.Message}");
            }
        }

        // ═══ 加载遮罩层 ═══
        private void StartLoadingSpinner()
        {
            var rotateAnim = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1.2))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            loadingSpinner.RenderTransformOrigin = new Point(0.5, 0.5);
            loadingSpinner.RenderTransform = new RotateTransform(0);
            loadingSpinner.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);

            AnimationHelper.PulseScale(loadingIconContainer, 1.0, 1.05, 2.0);
        }

        public void ShowLoading(string message = "加载中...", string subMessage = "")
        {
            if (loadingOverlay == null) return;
            txtLoadingMessage.Text = message;
            txtLoadingSub.Text = subMessage;
            loadingOverlay.Visibility = Visibility.Visible;

            // 图标容器的呼吸脉冲
            loadingIconContainer.Opacity = 1;
            loadingIconContainer.RenderTransformOrigin = new Point(0.5, 0.5);
            AnimationHelper.PulseScale(loadingIconContainer, 1.0, 1.05, 2.0);

            // 圆环旋转
            var rotateAnim = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            loadingSpinner.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);

            // 进度条动画
            var progressAnim = new DoubleAnimation(0, 0.85, TimeSpan.FromSeconds(3))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            loadingProgressBar.BeginAnimation(WidthProperty, progressAnim);

            // 加载遮罩淡入
            loadingOverlay.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
        }

        public void HideLoading()
        {
            if (loadingOverlay == null) return;

            // 进度条快速填满
            loadingProgressBar.BeginAnimation(WidthProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));

            // 延迟关闭以显示进度条满格
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                loadingOverlay.Visibility = Visibility.Collapsed;
                loadingSpinner.BeginAnimation(RotateTransform.AngleProperty, null);
                loadingIconContainer.BeginAnimation(OpacityProperty, null);
            };
            timer.Start();
        }

        // ═══ 托盘图标 ═══
        private void InitTrayIcon()
        {
            try
            {
                _trayIcon = new NotifyIconManager(this);
                _trayIcon.Create();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"托盘图标初始化失败: {ex.Message}");
            }
        }

        // ═══ 全局快捷键 ═══
        private void InitHotkeys()
        {
            try
            {
                _hotkeys = new HotkeyManager(this);
                _hotkeys.Initialize();

                // F5: 快速开始匹配
                _hotkeys.Register(ModifierKeys.None, Key.F5, () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (App.AntiCheatBlocked)
                        {
                            DialogHelper.ShowAntiCheatBlocked(this);
                            return;
                        }
                        var quickMatch = new QuickMatchWindow { Owner = this };
                        quickMatch.ShowDialog();
                    });
                });

                // F6: 切换语音静音
                _hotkeys.Register(ModifierKeys.None, Key.F6, () =>
                {
                    Dispatcher.Invoke(() => ShowToast("🎤 语音已切换"));
                });

                // F8: 显示/隐藏覆盖层
                _hotkeys.Register(ModifierKeys.None, Key.F8, () =>
                {
                    Dispatcher.Invoke(() => ShowToast("覆盖层已切换"));
                });

                // Ctrl+Shift+M: 显示主窗口
                _hotkeys.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.M, () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _trayIcon?.ShowWindow();
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"快捷键初始化失败: {ex.Message}");
            }
        }

        // ═══ 显示设置 ═══
        public void ApplyDisplaySettings()
        {
            try
            {
                var settings = AppSettings.Load();
                ApplyScaleTransform(settings.UiScale);

                // 侧栏折叠已移除——改用固定宽度
            }
            catch { }
        }

        /// <summary>
        /// 即时应用缩放变换（由设置窗口即时调用）
        /// </summary>
        public void ApplyScaleTransform(double scale)
        {
            try
            {
                if (Math.Abs(scale - 1.0) > 0.01)
                {
                    contentArea.LayoutTransform = new ScaleTransform(scale, scale);
                }
                else
                {
                    contentArea.LayoutTransform = null;
                }
            }
            catch { }
        }

        // Toast通知系统
        /// <summary>
        /// 登录成功通知
        /// </summary>
        private void ShowLoginNotification()
        {
            var name = App.CurrentUser?.Username ?? "玩家";
            ShowToast($"👋 欢迎回来，{name}！");
        }

        /// <summary>
        /// 登录后检测节日和生日（延迟执行）
        /// </summary>
        private async void CheckHolidayAndBirthdayAsync()
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(3000);

                var settings = AppSettings.Load();

                // 节日检测
                var holiday = await Services.HolidayService.GetTodayHolidayAsync();
                if (holiday != null)
                {
                    var holidayKey = $"{DateTime.Now:yyyy-MM-dd}_{holiday.Name}";
                    var permanentKey = "PERMANENT_" + holiday.Name;
                    if (settings.LastDismissedHoliday != holidayKey && settings.LastDismissedHoliday != permanentKey)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            var popup = new HolidayPopupWindow(holiday);
                            popup.ShowDialog();
                        });
                    }
                }

                // 生日检测
                if (App.CurrentUser != null && !string.IsNullOrEmpty(App.SavedAuthToken))
                {
                    var profileResult = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/auth/profile");
                    if (profileResult.Success)
                    {
                        var userId = profileResult.Data.TryGetProperty("id", out var id) ? id.GetInt64() : 0L;
                        if (userId > 0)
                        {
                            var birthdayResult = await Services.HolidayService.CheckBirthdayAsync(userId);
                            if (birthdayResult?.IsBirthday == true)
                            {
                                var birthdayKey = $"{DateTime.Now:yyyy-MM-dd}_生日";
                                if (settings.LastDismissedHoliday != birthdayKey)
                                {
                                    await Dispatcher.InvokeAsync(() =>
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

        public void ShowToast(string message)
        {
            var toast = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 14, 20, 14),
                Margin = new Thickness(0, 0, 24, 24),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Opacity = 0,
                RenderTransform = new TranslateTransform(100, 0)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                FontSize = 14
            };

            toast.Child = textBlock;

            // 添加到窗口
            var grid = this.Content as Grid;
            if (grid != null)
            {
                Grid.SetColumnSpan(toast, 2);
                Grid.SetRowSpan(toast, 2);
                grid.Children.Add(toast);

                // 进入动画（直接 BeginAnimation，不走 Storyboard 属性路径）
                toast.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
                toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(100, 0, TimeSpan.FromSeconds(0.4))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });

                // 3秒后自动消失
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, _) =>
                {
                    timer.Stop();
                    var opacityOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                    opacityOut.Completed += (s2, _) => grid.Children.Remove(toast);
                    toast.BeginAnimation(UIElement.OpacityProperty, opacityOut);
                    toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                        new DoubleAnimation(0, 100, TimeSpan.FromSeconds(0.3)));
                };
                timer.Start();
            }
        }
    }

    public class MatchResult
    {
        public string Map { get; set; } = "";
        public string Date { get; set; } = "";
        public string Score { get; set; } = "";
        public Brush ResultColor { get; set; } = Brushes.Gray;
    }
}
