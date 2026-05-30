using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace BeiShuiCS2
{
    public partial class ServerManagerWindow : Window
    {
        public ObservableCollection<ServerInfo> Servers { get; set; } = new();
        private string configFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servers.json");
        private bool _isServersTab = true;
        private bool _dataTabInitialized;

        public ServerManagerWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };
            LoadServers();
        }

        // ═══════════════════════════════════════════════════════════════
        // 入口：30+ 种原创交互动画（拒绝敷衍）
        // ═══════════════════════════════════════════════════════════════
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtTitle.Text = "管理中心";
            AnimationHelper.CreateFloatingParticles(particleCanvas, 6);

            // Simple stagger entrance
            var items = new FrameworkElement[] { headerIcon, txtTitle, txtSubtitle, tabBar, listHeader, selectionBar, btnAdd, btnEdit, btnDelete, btnConnect, launchBtnWrapper };
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                items[i].Opacity = 0;
                items[i].RenderTransformOrigin = new Point(0.5, 0.5);
                items[i].RenderTransform = new TranslateTransform(0, 15);

                int idx = i;
                items[i].BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                    { BeginTime = TimeSpan.FromSeconds(0.1 + idx * 0.04), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                items[i].RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(15, 0, TimeSpan.FromSeconds(0.35))
                    { BeginTime = TimeSpan.FromSeconds(0.1 + idx * 0.04), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }

            // Keep the glow pulse on launch button
            AnimationHelper.AttachGlowPulse(btnLaunchCS2, Color.FromRgb(0x4A, 0xDE, 0x80), 0.25, 0.8, 2.2);
            AnimationHelper.AttachMagneticHover(btnLaunchCS2, 1.03);

            AddStatusDotPulse();
            LoadDataPanel();
        }

        // ═══════════════════════════════════════════════════════════════
        // Tab 切换 — 弹性过渡动画
        // ═══════════════════════════════════════════════════════════════
        private void TabServers_Click(object sender, RoutedEventArgs e)
        {
            if (_isServersTab) return;
            SwitchTabWithAnimation(panelData, panelServers,
                () =>
                {
                    if (serverListPanel.Children.Count > 0)
                        AnimationHelper.AnimeStagger(serverListPanel.Children, "up", 20, 0.1, 0.05, 0.3);
                },
                tabServers, tabData);
            _isServersTab = true;
        }

        private void TabData_Click(object sender, RoutedEventArgs e)
        {
            if (!_isServersTab) return;
            SwitchTabWithAnimation(panelServers, panelData,
                () =>
                {
                    var dataCards = new FrameworkElement[] { cardScreenshots, cardDemos, cardCache, cardDisk, tipFooter };
                    for (int i = 0; i < dataCards.Length; i++)
                    {
                        dataCards[i].BeginAnimation(OpacityProperty, null);
                        if (dataCards[i].RenderTransform is TranslateTransform tt)
                        {
                            tt.BeginAnimation(TranslateTransform.XProperty, null);
                            tt.BeginAnimation(TranslateTransform.YProperty, null);
                        }
                        AnimationHelper.SlideIn(dataCards[i], 50, 0, 0.4, 0.12 + 0.08 * i);
                    }
                    AnimateDiskBar();
                    AnimateCacheCounter();
                    if (!_dataTabInitialized)
                    {
                        var cardIcons = new[] { cardScreenshots, cardDemos, cardCache, cardDisk };
                        foreach (var card in cardIcons)
                        {
                            var iconBorder = FindVisualChildren<Border>(card).FirstOrDefault(b => b.Width == 40);
                            if (iconBorder != null)
                                AnimationHelper.FloatingAnimation(iconBorder, 4, 4, 0);
                        }
                        _dataTabInitialized = true;
                    }
                    RefreshDataInfo();
                },
                tabData, tabServers);
            _isServersTab = false;
        }

        /// <summary>
        /// 统一 Tab 切换过渡：旧面板缩出 → 指示条滑动 → 新面板弹性弹入
        /// </summary>
        private void SwitchTabWithAnimation(Grid oldPanel, Grid newPanel,
            Action onNewPanelShown, Button activeTab, Button inactiveTab)
        {
            // 1. 旧面板缩出（150ms）
            oldPanel.RenderTransformOrigin = new Point(0.5, 0.5);
            oldPanel.RenderTransform = new ScaleTransform(1, 1);
            oldPanel.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(1, 0.85, TimeSpan.FromSeconds(0.15))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
            oldPanel.BeginAnimation(OpacityProperty,
                new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.12)));

            // 2. 延迟 150ms 后显示新面板
            var transitionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            transitionTimer.Tick += (_, _) =>
            {
                transitionTimer.Stop();
                oldPanel.Visibility = Visibility.Collapsed;
                oldPanel.BeginAnimation(OpacityProperty, null);
                oldPanel.RenderTransform = Transform.Identity;

                newPanel.Visibility = Visibility.Visible;
                newPanel.BeginAnimation(OpacityProperty, null);
                newPanel.RenderTransformOrigin = new Point(0.5, 0);
                newPanel.RenderTransform = new ScaleTransform(1, 0.85);
                newPanel.Opacity = 0;
                newPanel.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25))
                    { BeginTime = TimeSpan.FromSeconds(0.05), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                newPanel.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(0.85, 1, TimeSpan.FromSeconds(0.3))
                    {
                        BeginTime = TimeSpan.FromSeconds(0.05),
                        EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 6, EasingMode = EasingMode.EaseOut }
                    });

                var contentTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                contentTimer.Tick += (_, _) =>
                {
                    contentTimer.Stop();
                    onNewPanelShown?.Invoke();
                };
                contentTimer.Start();
            };
            transitionTimer.Start();

            // 3. 切换 Tab 视觉（按钮样式 + 指示条滑动）
            SwitchTabVisual(activeTab, inactiveTab);
        }

        private void SwitchTabVisual(Button activeTab, Button inactiveTab)
        {
            var inactiveBg = new SolidColorBrush(Color.FromRgb(0x15, 0x2A, 0x1C));

            // 激活标签：主色渐变背景 + 弹性缩放 + 白字
            activeTab.BeginAnimation(OpacityProperty, null);
            activeTab.Background = (Brush)FindResource("PrimaryGradientBrush");
            activeTab.Foreground = Brushes.White;
            activeTab.RenderTransformOrigin = new Point(0.5, 0.5);
            activeTab.RenderTransform = new ScaleTransform(0.92, 0.92);
            activeTab.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(0.35))
                { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
            activeTab.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(0.35))
                { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });

            // 非激活标签：柔和暗背景 + 次级文字色（永不 Transparent，永不消失）
            inactiveTab.Background = inactiveBg;
            inactiveTab.Foreground = (Brush)FindResource("TextSecondaryBrush");
            inactiveTab.RenderTransformOrigin = new Point(0.5, 0.5);
            inactiveTab.RenderTransform = new ScaleTransform(1, 1);
            inactiveTab.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            inactiveTab.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            // 指示条跟随激活标签
            int targetCol = activeTab == tabServers ? 0 : 1;
            Grid.SetColumn(tabIndicator, targetCol);
        }

        // ═══════════════════════════════════════════════════════════════
        // 服务器列表渲染
        // ═══════════════════════════════════════════════════════════════
        private void LoadServers()
        {
            if (File.Exists(configFile))
            {
                try
                {
                    string json = File.ReadAllText(configFile);
                    var list = JsonSerializer.Deserialize<ObservableCollection<ServerInfo>>(json);
                    if (list != null) Servers = list;
                }
                catch { Servers = new ObservableCollection<ServerInfo>(); }
            }

            if (Servers.Count == 0)
            {
                Servers.Add(new ServerInfo { Address = "127.0.0.1:27015", Name = "本地服务器" });
                Servers.Add(new ServerInfo { Address = "192.168.1.100:27015", Name = "示例服务器" });
            }

            RenderServers();
        }

        private void SaveServers()
        {
            try
            {
                string json = JsonSerializer.Serialize(Servers,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFile, json);
            }
            catch (Exception ex)
            {
                ShowToast($"保存服务器列表失败：{ex.Message}");
            }
        }

        private void RenderServers()
        {
            serverListPanel.Children.Clear();
            for (int i = 0; i < Servers.Count; i++)
            {
                var item = CreateServerItem(Servers[i], i);
                serverListPanel.Children.Add(item);
            }

            // 交错入场：每个卡片从下方淡入滑上 (增删后重播)
            if (serverListPanel.Children.Count > 0)
            {
                int i = 0;
                foreach (var child in serverListPanel.Children)
                {
                    if (child is FrameworkElement fe)
                    {
                        fe.Opacity = 0;
                        var tt = new TranslateTransform(0, 18);
                        fe.RenderTransform = tt;
                        int idx = i;
                        fe.BeginAnimation(OpacityProperty,
                            new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                            { BeginTime = TimeSpan.FromSeconds(idx * 0.05), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                        tt.BeginAnimation(TranslateTransform.YProperty,
                            new DoubleAnimation(18, 0, TimeSpan.FromSeconds(0.35))
                            { BeginTime = TimeSpan.FromSeconds(idx * 0.05), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    }
                    i++;
                }
            }

            AddStatusDotPulse();
            AttachTiltEffect();
        }

        private Border CreateServerItem(ServerInfo server, int index)
        {
            var item = new Border
            {
                Style = (Style)FindResource("ServerItemCard"),
                Tag = server,
                Cursor = Cursors.Hand
            };

            // 使用简单的纯色悬停动画（XAML ServerItemCard 样式已处理）
            item.MouseLeftButtonDown += (s, e) =>
            {
                if (s is Border b && b.Tag is ServerInfo si)
                {
                    txtSelectedIP.Text = si.Address;
                    // 23. 选中弹性脉冲
                    AnimationHelper.NumberPop(txtSelectedIP, 1.5);
                    HighlightSelected(b);
                }
            };

            item.MouseDown += (s, e) =>
            {
                if (e.ClickCount == 2 && s is Border b && b.Tag is ServerInfo si)
                {
                    txtSelectedIP.Text = si.Address;
                    ConnectToServer(si.Address);
                }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 名称
            var nameText = new TextBlock
            {
                Text = server.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 0);

            // 地址
            var addrText = new TextBlock
            {
                Text = server.Address,
                FontSize = 13,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontFamily = (FontFamily)FindResource("FontMono"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(addrText, 1);

            // 状态指示
            var statusPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = (Brush)FindResource("PrimaryBrush"),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };
            // 24. 状态点呼吸脉冲
            AnimationHelper.PulseScale(dot, 1.0, 1.3, 1.5);

            var statusText = new TextBlock
            {
                Text = "可用",
                FontSize = 12,
                Foreground = (Brush)FindResource("PrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusPanel.Children.Add(dot);
            statusPanel.Children.Add(statusText);
            Grid.SetColumn(statusPanel, 2);

            grid.Children.Add(nameText);
            grid.Children.Add(addrText);
            grid.Children.Add(statusPanel);
            item.Child = grid;

            // 25. 卡片鼠标跟随倾斜（对内容 Grid 应用 RotateTransform，不干扰 Border 的 RenderTransform）
            var tiltRotate = new RotateTransform(0);
            grid.RenderTransformOrigin = new Point(0.5, 0.5);
            grid.RenderTransform = tiltRotate;

            // 鼠标倾斜事件
            item.MouseMove += (s, e) =>
            {
                try
                {
                    var pos = e.GetPosition(item);
                    double w = item.ActualWidth;
                    double h = item.ActualHeight;
                    if (w <= 1 || h <= 1) return;
                    double angle = (pos.X / w - 0.5) * 4; // -2° ~ +2°
                    tiltRotate.BeginAnimation(RotateTransform.AngleProperty,
                        new DoubleAnimation(angle, TimeSpan.FromMilliseconds(100))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
                catch { }
            };
            item.MouseLeave += (_, _) =>
            {
                tiltRotate.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            };

            return item;
        }

        private void AddStatusDotPulse()
        {
            foreach (var child in serverListPanel.Children)
            {
                if (child is Border b && b.Child is Grid g)
                {
                    var dot = g.Children.OfType<StackPanel>()
                        .SelectMany(sp => sp.Children.OfType<Ellipse>())
                        .FirstOrDefault();
                    if (dot != null && dot.RenderTransform is ScaleTransform)
                    {
                        AnimationHelper.PulseScale(dot, 1.0, 1.3, 1.5);
                    }
                }
            }
        }

        private void HighlightSelected(Border selected)
        {
            foreach (var child in serverListPanel.Children)
            {
                if (child is Border b)
                {
                    b.ClearValue(Border.BackgroundProperty);
                    b.ClearValue(Border.BorderBrushProperty);
                    b.BorderThickness = new Thickness(1);
                }
            }
            selected.Background = (Brush)FindResource("BgHoverBrush");
            selected.BorderBrush = (Brush)FindResource("PrimaryBrush");
            selected.BorderThickness = new Thickness(1, 1, 1, 1);
        }

        // ═══════════════════════════════════════════════════════════════
        // 服务器 CRUD — 带动画
        // ═══════════════════════════════════════════════════════════════
        private void AddServer_Click(object sender, RoutedEventArgs e)
        {
            // 25. 按钮涟漪
            TryRipple(btnAdd, Color.FromRgb(0x4A, 0xDE, 0x80));
            AnimationHelper.ButtonPressPulse(btnAdd);

            var dialog = new InputDialogWindow("添加服务器", "输入服务器IP地址（IP:端口）", "");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                var newServer = new ServerInfo
                {
                    Address = dialog.InputText.Trim(),
                    Name = $"服务器 {Servers.Count + 1}"
                };
                Servers.Add(newServer);

                var item = CreateServerItem(newServer, Servers.Count - 1);
                serverListPanel.Children.Add(item);

                // 26. 新项弹性弹跳入场（仅 BounceIn，避免 SlideIn 覆盖 RenderTransform）
                AnimationHelper.BounceIn(item, 0.3, 0.6, 0.05);

                // 选择新添加的服务器
                txtSelectedIP.Text = newServer.Address;
                AnimationHelper.NumberPop(txtSelectedIP, 1.5);
                HighlightSelected(item);

                SaveServers();
            }
        }

        private void EditServer_Click(object sender, RoutedEventArgs e)
        {
            TryRipple(btnEdit, Color.FromRgb(0x60, 0xA5, 0xFA));
            AnimationHelper.ButtonPressPulse(btnEdit);

            if (GetSelectedServer() is ServerInfo selected)
            {
                var dialog = new InputDialogWindow("修改服务器", "修改服务器地址", selected.Address);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
                {
                    selected.Address = dialog.InputText.Trim();
                    SaveServers();
                    RenderServers();
                }
            }
            else
            {
                ShowToast("请先在列表中点击选择一个服务器");
            }
        }

        private void DeleteServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TryRipple(btnDelete, Color.FromRgb(0xFB, 0x71, 0x85));
                AnimationHelper.ButtonPressPulse(btnDelete);

                if (GetSelectedServer() is ServerInfo selected)
                {
                    var dialog = new ConfirmDialogWindow(
                        "删除服务器",
                        $"确定要删除服务器 \"{selected.Name}\" ({selected.Address}) 吗？\n\n此操作不可撤销，服务器将从列表中永久移除。",
                        "确认删除");
                    dialog.Owner = this;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    if (dialog.ShowDialog() == true && dialog.Confirmed)
                    {
                        if (txtSelectedIP.Text == selected.Address)
                            txtSelectedIP.Text = "未选择";

                        // 先删除数据（确保数据一致性）
                        Servers.Remove(selected);
                        SaveServers();

                        // 再播放缩出动画（纯装饰性），然后重建列表
                        var targetItem = serverListPanel.Children
                            .OfType<Border>()
                            .FirstOrDefault(b => b.Tag is ServerInfo si && si.Address == selected.Address);
                        if (targetItem != null)
                        {
                            // 缩小 + 淡出（快速动画，200ms）
                            targetItem.RenderTransformOrigin = new Point(0.5, 0.5);
                            targetItem.RenderTransform = new ScaleTransform(1, 1);
                            targetItem.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                                new DoubleAnimation(1, 0.3, TimeSpan.FromSeconds(0.12)));
                            targetItem.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                                new DoubleAnimation(1, 0.3, TimeSpan.FromSeconds(0.12)));
                            targetItem.BeginAnimation(OpacityProperty,
                                new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.12)));
                        }

                        // 延迟重建列表（让动画有时间播放，即使中断也不影响数据）
                        var rebuildTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(250)
                        };
                        rebuildTimer.Tick += (_, _) =>
                        {
                            rebuildTimer.Stop();
                            RenderServers();
                            ShowToast("服务器已删除");
                        };
                        rebuildTimer.Start();
                    }
                }
                else
                {
                    ShowToast("请先在列表中点击选择一个服务器");
                }
            }
            catch (Exception ex)
            {
                ShowToast($"删除服务器时出错：{ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // CS2 连接 — 使用 steam://rungameid/730//+connect
        // ═══════════════════════════════════════════════════════════════
        private void ConnectServer_Click(object sender, RoutedEventArgs e)
        {
            var sourceBtn = sender as Button;
            if (sourceBtn != null)
            {
                TryRipple(sourceBtn, Color.FromRgb(0x4A, 0xDE, 0x80));
                AnimationHelper.ButtonPressPulse(sourceBtn);
            }

            string ip = txtSelectedIP.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip) || ip == "未选择")
            {
                if (Servers.Count > 0)
                {
                    ip = Servers[0].Address;
                    txtSelectedIP.Text = ip;
                }
                else
                {
                    ShowToast("请先添加一个服务器地址");
                    return;
                }
            }

            ConnectToServer(ip);
        }

        private void ConnectToServer(string ip)
        {
            try
            {
                btnLaunchCS2.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(1, 0.7, TimeSpan.FromSeconds(0.3))
                    { AutoReverse = true });

                var mainWin = Application.Current.MainWindow as MainWindow;

                if (App.AntiCheatBlocked)
                {
                    mainWin?.ShowToast("⛔ 反作弊已封锁，无法启动游戏");
                    DialogHelper.ShowAntiCheatBlocked(this);
                    return;
                }

                // 启动游戏 + 心跳
                GameLauncher.ConnectAndHeartbeat(ip);

                // 弹出反作弊心跳验证窗口
                Dispatcher.Invoke(() =>
                {
                    var heartbeatWin = new AntiCheatHeartbeatWindow { Owner = this };
                    bool? result = heartbeatWin.ShowDialog();
                    if (result != true)
                    {
                        mainWin?.ShowToast("⚠️ 心跳验证失败，已断开连接");
                        GameLauncher.StopHeartbeat();
                        return;
                    }
                    mainWin?.ShowToast("✅ 反作弊心跳验证通过");
                });

                this.Close();
            }
            catch (Exception ex)
            {
                ShowToast($"连接服务器失败：{ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 数据面板
        // ═══════════════════════════════════════════════════════════════
        private void LoadDataPanel()
        {
            string screenshotPath = DetectScreenshotPath();
            txtScreenshotPath.Text = screenshotPath;
            txtScreenshotPath.Foreground = Directory.Exists(Path.GetDirectoryName(screenshotPath))
                ? (Brush)FindResource("TextSecondaryBrush")
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));

            string demoPath = DetectDemoPath();
            txtDemoPath.Text = demoPath;
            txtDemoPath.Foreground = Directory.Exists(Path.GetDirectoryName(demoPath))
                ? (Brush)FindResource("TextSecondaryBrush")
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
        }

        private void RefreshDataInfo()
        {
            UpdateCacheSize();
            UpdateDiskUsage();
        }

        private string DetectScreenshotPath()
        {
            string[] candidates = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Steam", "screenshots"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Counter-Strike Global Offensive", "screenshots"),
            };
            foreach (var path in candidates)
            {
                string dir = Path.GetDirectoryName(path) ?? "";
                if (Directory.Exists(dir)) return path;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\Steam\cs2_screenshots";
        }

        private string DetectDemoPath()
        {
            string[] candidates = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            };
            foreach (var path in candidates)
            {
                if (Directory.Exists(path)) return path;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        }

        private void UpdateCacheSize()
        {
            try
            {
                string cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BeiShuiCS2", "Logs");
                long size = 0;
                if (Directory.Exists(cacheDir))
                {
                    size = Directory.EnumerateFiles(cacheDir, "*", SearchOption.AllDirectories)
                        .Sum(f => new FileInfo(f).Length);
                }
                string sizeStr = size > 1024 * 1024
                    ? $"缓存大小: {size / (1024 * 1024)} MB"
                    : $"缓存大小: {size / 1024} KB";
                txtCacheSize.Text = sizeStr;
            }
            catch { txtCacheSize.Text = "缓存大小: 未知"; }
        }

        private void AnimateCacheCounter()
        {
            string text = txtCacheSize.Text;
            // 28. 缓存数字弹性弹出
            AnimationHelper.NumberPop(txtCacheSize, 1.6);
        }

        private void UpdateDiskUsage()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => appDir.StartsWith(d.Name, StringComparison.OrdinalIgnoreCase));

                if (drive != null && drive.IsReady)
                {
                    long freeGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                    long totalGB = drive.TotalSize / (1024 * 1024 * 1024);
                    int usedPercent = (int)((1.0 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100);

                    txtDiskUsage.Text = $"{usedPercent}% 已用 ({totalGB - freeGB} GB / {totalGB} GB)";
                    diskUsageBar.Width = 0;
                }
                else
                {
                    txtDiskUsage.Text = "无法获取磁盘信息";
                }
            }
            catch { txtDiskUsage.Text = "无法获取磁盘信息"; }
        }

        private void AnimateDiskBar()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => appDir.StartsWith(d.Name, StringComparison.OrdinalIgnoreCase));

                if (drive != null && drive.IsReady)
                {
                    int usedPercent = (int)((1.0 - (double)drive.AvailableFreeSpace / drive.TotalSize) * 100);
                    double targetWidth = 280 * usedPercent / 100.0;

                    // 29. 磁盘进度条宽度动画 (0 → targetWidth)
                    var widthAnim = new DoubleAnimation(0, targetWidth, TimeSpan.FromSeconds(0.8));
                    widthAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                    diskUsageBar.BeginAnimation(Border.WidthProperty, widthAnim);
                }
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════════
        // 工具方法
        // ═══════════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════════
        // 3D 鼠标跟随倾斜效果（已内联在 CreateServerItem 中）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 确保现有卡片已附加倾斜效果（tilt 已内联在 CreateServerItem 中，此方法用于防御性刷新）
        /// </summary>
        private void AttachTiltEffect()
        {
            // Tilt 效果已在 CreateServerItem 中通过内联 lambdas 应用，无需额外操作
        }

        private void TryRipple(Button btn, Color color)
        {
            try { AnimationHelper.RippleEffect(btn, color); }
            catch { /* ripple 静默失败 */ }
        }

        private ServerInfo? GetSelectedServer()
        {
            string currentIP = txtSelectedIP.Text;
            if (!string.IsNullOrEmpty(currentIP) && currentIP != "未选择")
                return Servers.FirstOrDefault(s => s.Address == currentIP);
            return null;
        }

        private void OpenScreenshotFolder_Click(object sender, RoutedEventArgs e)
        {
            TryRipple((Button)sender, Color.FromRgb(0x60, 0xA5, 0xFA));
            string path = txtScreenshotPath.Text;
            string dir = Path.GetDirectoryName(path) ?? path;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }

        private void OpenDemoFolder_Click(object sender, RoutedEventArgs e)
        {
            TryRipple((Button)sender, Color.FromRgb(0x22, 0xD3, 0xEE));
            string path = txtDemoPath.Text;
            string dir = Path.GetDirectoryName(path) ?? path;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }

        private void CleanScreenshots_Click(object sender, RoutedEventArgs e)
        {
            TryRipple((Button)sender, Color.FromRgb(0xFB, 0x71, 0x85));
            var dialog = new ConfirmDialogWindow("清理截图",
                "确定要清理 CS2 截图文件夹吗？\n此操作将删除所有截图文件。", "确认清理");
            dialog.Owner = this;
            if (dialog.ShowDialog() != true || !dialog.Confirmed) return;

            string path = txtScreenshotPath.Text;
            string dir = Path.GetDirectoryName(path) ?? path;
            if (!Directory.Exists(dir)) { ShowToast("截图目录不存在"); return; }

            try
            {
                var files = Directory.GetFiles(dir);
                foreach (var file in files) { try { File.Delete(file); } catch { } }
                ShowToast($"已清理 {files.Length} 个截图文件");
            }
            catch (Exception ex) { ShowToast($"清理失败: {ex.Message}"); }
        }

        private void CleanCache_Click(object sender, RoutedEventArgs e)
        {
            TryRipple((Button)sender, Color.FromRgb(0xFB, 0x71, 0x85));
            var dialog = new ConfirmDialogWindow("清理缓存",
                "确定要清理本地缓存吗？\n包括日志文件等临时数据。", "确认清理");
            dialog.Owner = this;
            if (dialog.ShowDialog() != true || !dialog.Confirmed) return;

            try
            {
                string cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BeiShuiCS2", "Logs");
                if (Directory.Exists(cacheDir))
                {
                    foreach (var file in Directory.GetFiles(cacheDir))
                    { try { File.Delete(file); } catch { } }
                }
                UpdateCacheSize();
                ShowToast("缓存清理完成");
            }
            catch (Exception ex) { ShowToast($"清理失败: {ex.Message}"); }
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            TryRipple((Button)sender, Color.FromRgb(0x4A, 0xDE, 0x80));
            string exportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "BeiShuiCS2_Export");
            Directory.CreateDirectory(exportPath);
            try
            {
                string serverConfig = Path.Combine(exportPath, "servers.json");
                if (File.Exists(configFile)) File.Copy(configFile, serverConfig, true);
                ShowToast($"配置已导出到: {exportPath}");
                System.Diagnostics.Process.Start("explorer.exe", exportPath);
            }
            catch (Exception ex) { ShowToast($"导出失败: {ex.Message}"); }
        }

        private void ShowToast(string message)
        {
            var toast = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1F)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9)),
                FontSize = 14
            };
            toast.Child = textBlock;

            var root = this.Content as Grid;
            if (root != null)
            {
                root.Children.Add(toast);
                toast.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
                toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(100, 0, TimeSpan.FromSeconds(0.4))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                    fadeOut.Completed += (_, _) => root.Children.Remove(toast);
                    toast.BeginAnimation(OpacityProperty, fadeOut);
                    toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                        new DoubleAnimation(0, 100, TimeSpan.FromSeconds(0.3)));
                };
                timer.Start();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // 30. 窗口退出：缩放淡出
            AnimationHelper.WindowCloseWithScale(this);
        }
    }

    public class ServerInfo
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
    }
}
