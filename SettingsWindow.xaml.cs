using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings = null!;

        public SettingsWindow()
        {
            InitializeComponent();

            // 加载设置
            _settings = AppSettings.Load();

            // 延迟加载 UI 状态，避免事件在构造函数中意外触发 NullReference
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 窗口淡入动画
            this.Opacity = 1;
            try
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            }
            catch
            {
                // AllowsTransparency + BlurEffect 下动画可能失败，这是 WPF 框架限制
                this.Opacity = 1;
            }

            // 在代码后台设置所有系统信息（XAML 中 {x:Static} 在 .NET 9 下
            // 无法处理 OperatingSystem→string、bool→string、int→string 转换）
            txtOSVersion.Text = Environment.OSVersion.ToString();
            txtIs64Bit.Text = Environment.Is64BitProcess ? "64 位" : "32 位";
            txtProcessId.Text = Environment.ProcessId.ToString();

            LoadSettings();
            AnimationHelper.CreateFloatingParticles(particleCanvas, 12);

            // 生日下拉框初始化
            cmbBirthMonth.Items.Clear();
            for (int m = 1; m <= 12; m++)
                cmbBirthMonth.Items.Add(new ComboBoxItem { Content = $"{m}月", Tag = m.ToString("D2") });
            cmbBirthDay.Items.Clear();
            for (int d = 1; d <= 31; d++)
                cmbBirthDay.Items.Add(new ComboBoxItem { Content = $"{d}日", Tag = d.ToString("D2") });

            // 加载已保存的生日
            var settings = AppSettings.Load();
            txtServerStatus.Text = $"{settings.ServerUrl}:{settings.ServerPort}";
            if (App.CurrentUser != null)
            {
                // 从 profile 获取生日
                _ = LoadBirthdayFromProfile();
            }

            // 设置面板交错入场
            AnimateSettingsPanels();
        }

        private void AnimateSettingsPanels()
        {
            var panels = new FrameworkElement[] { panelGeneral, panelDisplay, panelGame };
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                int idx = i;
                Dispatcher.BeginInvoke(() =>
                {
                    if (panels[idx].Visibility != Visibility.Collapsed)
                    {
                        panels[idx].Opacity = 0;
                        panels[idx].RenderTransform = new TranslateTransform(0, 20);
                        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                        {
                            BeginTime = TimeSpan.FromSeconds(0.08 * idx),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        var slideUp = new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.4))
                        {
                            BeginTime = TimeSpan.FromSeconds(0.08 * idx),
                            EasingFunction = new BackEase { Amplitude = 0.15, EasingMode = EasingMode.EaseOut }
                        };
                        panels[idx].BeginAnimation(OpacityProperty, fadeIn);
                        panels[idx].RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideUp);
                    }
                }, DispatcherPriority.Loaded);
            }
        }

        private void DragMoveHandler(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.GetPosition(this).Y < 56) this.DragMove();
            }
            catch
            {
                // 某些窗口模板下 DragMove 可能抛出异常，静默忽略
            }
        }

        private void LoadSettings()
        {
            // CS2 路径
            if (txtCs2Path != null)
            {
                if (!string.IsNullOrEmpty(App.CS2Path))
                {
                    txtCs2Path.Text = App.CS2Path;
                    txtCs2Path.Foreground = (Brush)FindResource("TextPrimaryBrush");
                }
                else if (!string.IsNullOrEmpty(_settings.LastCS2Path))
                {
                    txtCs2Path.Text = _settings.LastCS2Path;
                    txtCs2Path.Foreground = (Brush)FindResource("TextPrimaryBrush");
                }
                else
                {
                    txtCs2Path.Text = "未检测到 CS2，请手动指定或前往 Steam 设置";
                    txtCs2Path.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                }
            }

            // 恢复通用设置
            if (toggleAutoStart != null) toggleAutoStart.IsChecked = _settings.AutoStartWithSystem;
            if (toggleAutoUpdate != null) toggleAutoUpdate.IsChecked = _settings.AutoUpdate;
            if (txtDownloadPath != null)
                txtDownloadPath.Text = string.IsNullOrEmpty(_settings.DownloadPath) ? "默认 (文档/BeiShui)" : _settings.DownloadPath;
            UpdateCacheSizeDisplay();

            if (cmbLanguage != null)
                {
                    foreach (ComboBoxItem item in cmbLanguage.Items)
                    {
                        if (item.Tag?.ToString() == _settings.Language)
                        {
                            cmbLanguage.SelectedItem = item;
                            break;
                        }
                    }
                }

                // 恢复显示设置
                if (cmbUiScale != null)
                {
                    foreach (ComboBoxItem item in cmbUiScale.Items)
                    {
                        if (item.Tag != null &&
                            double.TryParse(item.Tag.ToString(), out double tagVal) &&
                            Math.Abs(tagVal - _settings.UiScale) < 0.01)
                        {
                            cmbUiScale.SelectedItem = item;
                            break;
                        }
                    }
                }
                if (toggleAnimations != null) toggleAnimations.IsChecked = _settings.AnimationsEnabled;
                if (cmbAnimSpeed != null)
                {
                    foreach (ComboBoxItem item in cmbAnimSpeed.Items)
                    {
                        if (item.Tag?.ToString() == _settings.AnimationSpeed)
                        {
                            cmbAnimSpeed.SelectedItem = item;
                            break;
                        }
                    }
                }
                if (toggleCollapseSidebar != null) toggleCollapseSidebar.IsChecked = _settings.CollapseSidebar;

                // 恢复主题
                if (cmbTheme != null)
                {
                    foreach (ComboBoxItem item in cmbTheme.Items)
                    {
                        if (item.Tag?.ToString() == _settings.Theme)
                        {
                            cmbTheme.SelectedItem = item;
                            break;
                        }
                    }
                }
                ThemeManager.ApplyTheme(_settings.Theme == "dark");

                // 恢复隐私设置
                if (toggleDataCollection != null) toggleDataCollection.IsChecked = _settings.AllowDataCollection;
                if (toggleErrorReports != null) toggleErrorReports.IsChecked = _settings.SendErrorReports;
                if (toggleInvisibleMode != null) toggleInvisibleMode.IsChecked = _settings.InvisibleMode;

                // 恢复通知设置
                if (toggleMatchNotifs != null) toggleMatchNotifs.IsChecked = _settings.MatchNotifications;
                if (toggleFriendNotifs != null) toggleFriendNotifs.IsChecked = _settings.FriendNotifications;
                if (toggleSystemNotifs != null) toggleSystemNotifs.IsChecked = _settings.SystemNotifications;
                if (toggleDnd != null) toggleDnd.IsChecked = _settings.DndEnabled;

                // 恢复账号安全设置
                if (toggleLoginNotifs != null) toggleLoginNotifs.IsChecked = _settings.LoginNotifications;

                // 恢复游戏附加设置
                if (toggleMinimizeOnLaunch != null)
                    toggleMinimizeOnLaunch.IsChecked = _settings.StartMinimized;
                if (toggleInGameOverlay != null)
                    toggleInGameOverlay.IsChecked = _settings.AnimationsEnabled;
                if (txtLaunchArgs != null && !string.IsNullOrEmpty(_settings.LaunchArgs))
                    txtLaunchArgs.Text = _settings.LaunchArgs;

                // 恢复 IPv6 设置
                if (toggleIPv6Check != null)
                    toggleIPv6Check.IsChecked = _settings.IPv6AutoEnable;

                // 为所有没有 Checked/Unchecked 事件的开关添加保存处理器
                var allToggles = new[] {
                    toggleAutoUpdate, toggleAnimations, toggleCollapseSidebar,
                    toggleDataCollection, toggleErrorReports, toggleInvisibleMode,
                    toggleMatchNotifs, toggleFriendNotifs, toggleSystemNotifs, toggleDnd,
                    toggleLoginNotifs, toggleMinimizeOnLaunch, toggleInGameOverlay,
                    toggleIPv6Check
                };
                // IPv6 开关单独绑定事件
                if (toggleIPv6Check != null)
                {
                    toggleIPv6Check.Checked += ToggleIPv6Check_Changed;
                    toggleIPv6Check.Unchecked += ToggleIPv6Check_Changed;
                }
                foreach (var toggle in allToggles)
                {
                    if (toggle != null)
                    {
                        toggle.Checked += Toggle_Changed;
                        toggle.Unchecked += Toggle_Changed;
                    }
                }

                // 为动画速度下拉添加事件
                if (cmbAnimSpeed != null)
                    cmbAnimSpeed.SelectionChanged += CmbAnimSpeed_Changed;


            this.MouseLeftButtonDown += DragMoveHandler;
        }

        private void Toggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            // 同步所有开关状态到设置对象
            if (toggleAutoUpdate != null) _settings.AutoUpdate = toggleAutoUpdate.IsChecked == true;
            if (toggleAnimations != null) _settings.AnimationsEnabled = toggleAnimations.IsChecked == true;
            if (toggleCollapseSidebar != null) _settings.CollapseSidebar = toggleCollapseSidebar.IsChecked == true;
            if (toggleDataCollection != null) _settings.AllowDataCollection = toggleDataCollection.IsChecked == true;
            if (toggleErrorReports != null) _settings.SendErrorReports = toggleErrorReports.IsChecked == true;
            if (toggleInvisibleMode != null) _settings.InvisibleMode = toggleInvisibleMode.IsChecked == true;
            if (toggleMatchNotifs != null) _settings.MatchNotifications = toggleMatchNotifs.IsChecked == true;
            if (toggleFriendNotifs != null) _settings.FriendNotifications = toggleFriendNotifs.IsChecked == true;
            if (toggleSystemNotifs != null) _settings.SystemNotifications = toggleSystemNotifs.IsChecked == true;
            if (toggleDnd != null) _settings.DndEnabled = toggleDnd.IsChecked == true;
            if (toggleLoginNotifs != null) _settings.LoginNotifications = toggleLoginNotifs.IsChecked == true;
            if (toggleMinimizeOnLaunch != null) _settings.StartMinimized = toggleMinimizeOnLaunch.IsChecked == true;
            if (toggleInGameOverlay != null) _settings.AnimationsEnabled = toggleInGameOverlay.IsChecked == true;
            _settings.Save();
        }

        private void CmbAnimSpeed_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null || cmbAnimSpeed == null) return;
            if (cmbAnimSpeed.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                _settings.AnimationSpeed = item.Tag.ToString()!;
                _settings.Save();
            }
        }

        private void SwitchTab(StackPanel activePanel, Button activeTab)
        {
            if (_settings == null) return;

            // 隐藏所有面板
            panelGeneral.Visibility = Visibility.Collapsed;
            panelDisplay.Visibility = Visibility.Collapsed;
            panelGame.Visibility = Visibility.Collapsed;
            panelPrivacy.Visibility = Visibility.Collapsed;
            panelNotifications.Visibility = Visibility.Collapsed;
            if (panelHotkeys != null) panelHotkeys.Visibility = Visibility.Collapsed;
            if (panelMiniGames != null) panelMiniGames.Visibility = Visibility.Collapsed;
            if (panelTools != null) panelTools.Visibility = Visibility.Collapsed;
            panelAccount.Visibility = Visibility.Collapsed;
            panelAbout.Visibility = Visibility.Collapsed;

            // 重置所有标签样式
            var tabs = new[] { tabGeneral, tabDisplay, tabGame, tabPrivacy, tabNotifications, tabHotkeys, tabMiniGames, tabTools, tabAccount, tabAbout };
            foreach (var tab in tabs)
            {
                tab.Background = Brushes.Transparent;
                tab.BorderThickness = new Thickness(0, 0, 0, 0);
            }

            // 激活当前标签：背景色 + 左侧绿边
            activePanel.Visibility = Visibility.Visible;
            activeTab.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F3A28"));
            activeTab.BorderThickness = new Thickness(3, 0, 0, 0);
            activeTab.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));

            // 面板进入动画 (anime.js 风格弹性过渡)
            if (_settings.AnimationsEnabled)
            {
                try
                {
                    // 弹性淡入 + 上滑
                    activePanel.BeginAnimation(OpacityProperty,
                        new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    var transform = new TranslateTransform(0, 20);
                    activePanel.RenderTransform = transform;
                    transform.BeginAnimation(TranslateTransform.YProperty,
                        new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.35))
                        { EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 5, EasingMode = EasingMode.EaseOut } });

                    // 激活标签脉冲
                    if (activeTab.RenderTransform == null || activeTab.RenderTransform is not ScaleTransform)
                    {
                        activeTab.RenderTransformOrigin = new Point(0.5, 0.5);
                        activeTab.RenderTransform = new ScaleTransform(1, 1);
                    }
                    activeTab.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                        new DoubleAnimation(0.95, 1, TimeSpan.FromSeconds(0.25))
                        { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
                }
                catch
                {
                    // 动画失败时确保面板可见
                    activePanel.Opacity = 1;
                    activePanel.RenderTransform = Transform.Identity;
                }
            }
            else
            {
                activePanel.Opacity = 1;
                activePanel.RenderTransform = Transform.Identity;
            }
        }

        // ===== 标签页切换 =====
        private void TabGeneral_Click(object sender, RoutedEventArgs e) => SwitchTab(panelGeneral, tabGeneral);
        private void TabDisplay_Click(object sender, RoutedEventArgs e) => SwitchTab(panelDisplay, tabDisplay);
        private void TabGame_Click(object sender, RoutedEventArgs e) => SwitchTab(panelGame, tabGame);
        private void TabPrivacy_Click(object sender, RoutedEventArgs e) => SwitchTab(panelPrivacy, tabPrivacy);
        private void TabNotifications_Click(object sender, RoutedEventArgs e) => SwitchTab(panelNotifications, tabNotifications);
        private void TabHotkeys_Click(object sender, RoutedEventArgs e) => SwitchTab(panelHotkeys, tabHotkeys);
        private void TabMiniGames_Click(object sender, RoutedEventArgs e) => SwitchTab(panelMiniGames, tabMiniGames);
        private void TabTools_Click(object sender, RoutedEventArgs e) => SwitchTab(panelTools, tabTools);
        private void TabAccount_Click(object sender, RoutedEventArgs e) => SwitchTab(panelAccount, tabAccount);
        private void TabAbout_Click(object sender, RoutedEventArgs e) => SwitchTab(panelAbout, tabAbout);

        // ===== 服务器配置 =====
        private void OpenServerConfig_Click(object sender, RoutedEventArgs e)
        {
            var config = new ServerConfigWindow { Owner = this };
            config.ShowDialog();
            var settings = AppSettings.Load();
            txtServerStatus.Text = $"{settings.ServerUrl}:{settings.ServerPort}";
        }

        // ===== 生日 =====
        private async System.Threading.Tasks.Task LoadBirthdayFromProfile()
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/auth/profile");
                if (result.Success && result.Data.TryGetProperty("birthday", out var bd))
                {
                    var birthday = bd.GetString() ?? "";
                    if (!string.IsNullOrEmpty(birthday))
                    {
                        var parts = birthday.Split('-');
                        if (parts.Length == 2)
                        {
                            foreach (ComboBoxItem item in cmbBirthMonth.Items)
                                if (item.Tag?.ToString() == parts[0]) { cmbBirthMonth.SelectedItem = item; break; }
                            foreach (ComboBoxItem item in cmbBirthDay.Items)
                                if (item.Tag?.ToString() == parts[1]) { cmbBirthDay.SelectedItem = item; break; }
                        }
                    }
                }
            }
            catch { }
        }

        private async void SaveBirthday_Click(object sender, RoutedEventArgs e)
        {
            var month = (cmbBirthMonth.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var day = (cmbBirthDay.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (month == null || day == null)
            {
                ShowToast("请选择月份和日期");
                return;
            }

            var birthday = $"{month}-{day}";
            var result = await ApiClient.PostAsync<object>("/api/auth/profile", new
            {
                birthday = birthday
            });

            // 调用 PUT 而非 POST
            try
            {
                using var http = new System.Net.Http.HttpClient();
                var token = App.SavedAuthToken;
                if (!string.IsNullOrEmpty(token))
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var json = System.Text.Json.JsonSerializer.Serialize(new { birthday });
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var baseUrl = Services.NetworkHelper.BuildApiBaseUrl();
                var response = await http.PutAsync($"{baseUrl}/api/auth/profile", content);
                if (response.IsSuccessStatusCode)
                    ShowToast("✅ 生日已保存");
                else
                    ShowToast("❌ 保存失败");
            }
            catch (Exception ex)
            {
                ShowToast($"❌ 保存失败: {ex.Message}");
            }
        }

        // ===== 法律信息 =====
        private void ViewAgreement_Click(object sender, RoutedEventArgs e)
            => new LegalViewWindow(LegalType.Agreement) { Owner = this }.ShowDialog();
        private void ViewPrivacy_Click(object sender, RoutedEventArgs e)
            => new LegalViewWindow(LegalType.Privacy) { Owner = this }.ShowDialog();
        private void ViewDeclaration_Click(object sender, RoutedEventArgs e)
            => new LegalViewWindow(LegalType.Declaration) { Owner = this }.ShowDialog();

        // ===== 通用设置 =====
        private void ToggleAutoStart_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BeiShuiCS2.exe");
                    key.SetValue("BeiShuiCS2", $"\"{exePath}\"");
                    key.Close();
                }
                _settings.AutoStartWithSystem = true;
                _settings.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置开机自启失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ToggleAutoStart_Unchecked(object sender, RoutedEventArgs e)
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                key.DeleteValue("BeiShuiCS2", false);
                key.Close();
            }
            _settings.AutoStartWithSystem = false;
            _settings.Save();
        }

        private void BrowseDownloadPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择下载目录",
                Filter = "文件夹|*.none",
                CheckFileExists = false
            };
            if (dialog.ShowDialog() == true)
            {
                var dir = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(dir))
                {
                    txtDownloadPath.Text = dir;
                    _settings.DownloadPath = dir;
                    _settings.Save();
                }
            }
        }

        private void CleanCache_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要清理本地缓存数据吗？", "清理缓存",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // 模拟清理
                _settings.Save();
                txtCacheSize.Text = "当前缓存: 0 MB";
                MessageBox.Show("缓存清理完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateCacheSizeDisplay()
        {
            // 模拟显示缓存大小
            txtCacheSize.Text = "当前缓存: 128 MB";
        }

        // ===== 语言切换 =====
        private void Language_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null || cmbLanguage == null) return;
            if (cmbLanguage.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string lang = item.Tag.ToString()!;
                _settings.Language = lang;
                _settings.Save();
                LanguageManager.SetLanguage(lang);
                ShowToast(LanguageManager.GetString("LangChanged"));
            }
        }

        // ===== 显示设置 =====
        private void UiScale_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null || cmbUiScale == null) return;
            if (cmbUiScale.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                if (double.TryParse(item.Tag.ToString(), out double scale))
                {
                    _settings.UiScale = scale;
                    _settings.Save();

                    // 即时缩放当前窗口
                    if (Owner is MainWindow mainWin)
                    {
                        mainWin.ApplyScaleTransform(scale);
                    }
                    ShowToast("界面缩放已应用");
                }
            }
        }

        private void Theme_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_settings == null || cmbTheme == null) return;
            if (cmbTheme.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string theme = item.Tag.ToString()!;
                bool isDark = theme == "dark";
                ThemeManager.ApplyTheme(isDark);
                _settings.Theme = theme;
                _settings.Save();
            }
        }

        // ===== 游戏设置 =====
        private void BrowseCs2Path_Click(object sender, RoutedEventArgs e)
        {
            var pathSelector = new PathSelectionWindow { Owner = this };
            if (pathSelector.ShowDialog() == true && !string.IsNullOrEmpty(pathSelector.SelectedPath))
            {
                txtCs2Path.Text = pathSelector.SelectedPath;
                txtCs2Path.Foreground = (Brush)FindResource("TextPrimaryBrush");
                SteamHelper.SetCS2Path(pathSelector.SelectedPath);
                App.CS2Path = pathSelector.SelectedPath;
            }
        }

        private void RefreshCs2Path_Click(object sender, RoutedEventArgs e)
        {
            var path = SteamHelper.RefreshCS2Path();
            if (!string.IsNullOrEmpty(path))
            {
                txtCs2Path.Text = path;
                txtCs2Path.Foreground = (Brush)FindResource("TextPrimaryBrush");
                MessageBox.Show($"已自动找到 CS2：\n{path}", "检测成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("未能在常见位置找到 CS2，请手动选择路径。",
                    "未检测到", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveLaunchArgs_Click(object sender, RoutedEventArgs e)
        {
            _settings.LaunchArgs = txtLaunchArgs.Text;
            _settings.Save();
            MessageBox.Show("启动参数已保存。", "保存成功");
        }

        // ===== 账号设置 =====
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("密码修改功能开发中，请前往官网 beishui.com 修改。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UnbindSteam_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要解绑 Steam 账号吗？\n解绑后需要重新验证方可使用匹配功能。",
                "确认解绑", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Steam 账号已解绑。", "操作完成");
            }
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            var verify = new VerifyWindow { Owner = this };
            verify.ShowDialog();
        }

        private void LoginHistory_Click(object sender, RoutedEventArgs e)
        {
            var historyWin = new LoginHistoryWindow { Owner = this };
            historyWin.ShowDialog();
        }

        private void DeviceManagement_Click(object sender, RoutedEventArgs e)
        {
            var win = new DeviceManagementWindow { Owner = this };
            win.ShowDialog();
        }

        // ===== 小游戏 =====
        private int _guessNumber = new Random().Next(1, 101);
        private int _guessCount = 0;

        private void LaunchTicTacToe_Click(object sender, RoutedEventArgs e)
        {
            var game = new TicTacToeWindow { Owner = this };
            game.ShowDialog();
        }

        private void LaunchMemoryGame_Click(object sender, RoutedEventArgs e)
        {
            var game = new MemoryGameWindow { Owner = this };
            game.ShowDialog();
        }

        private void LaunchSnakeGame_Click(object sender, RoutedEventArgs e)
        {
            var game = new SnakeGameWindow { Owner = this };
            game.ShowDialog();
        }

        private void LaunchReactionGame_Click(object sender, RoutedEventArgs e)
        {
            var game = new ReactionGameWindow { Owner = this };
            game.ShowDialog();
        }

        private void GuessNumber_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtGuess.Text))
            {
                txtGuessResult.Text = "请输入一个数字！";
                txtGuessResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                return;
            }

            if (!int.TryParse(txtGuess.Text, out int guess) || guess < 1 || guess > 100)
            {
                txtGuessResult.Text = "请输入 1-100 之间的整数！";
                txtGuessResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                return;
            }

            _guessCount++;

            if (guess == _guessNumber)
            {
                txtGuessResult.Text = $"🎉 恭喜你猜对了！就是 {_guessNumber}！";
                txtGuessResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                txtGuessCount.Text = $"你用了 {_guessCount} 次猜中！点击「重新开始」再来一局。";
                txtGuess.IsEnabled = false;
            }
            else if (guess < _guessNumber)
            {
                txtGuessResult.Text = $"太小了！(第 {_guessCount} 次猜)";
                txtGuessResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                txtGuessCount.Text = $"已猜 {_guessCount} 次，继续加油！";
            }
            else
            {
                txtGuessResult.Text = $"太大了！(第 {_guessCount} 次猜)";
                txtGuessResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                txtGuessCount.Text = $"已猜 {_guessCount} 次，继续加油！";
            }

            txtGuess.Text = "";
        }

        private void ResetGuessGame_Click(object sender, RoutedEventArgs e)
        {
            _guessNumber = new Random().Next(1, 101);
            _guessCount = 0;
            txtGuess.Text = "";
            txtGuess.IsEnabled = true;
            txtGuessResult.Text = "已重置！猜一个 1-100 的数字";
            txtGuessResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            txtGuessCount.Text = "";
        }

        // ===== 工具 =====
        private void ToggleIPv6Check_Changed(object sender, RoutedEventArgs e)
        {
            // XAML 中已移除 Checked/Unchecked 事件，仅在 Loaded 后用代码绑定，
            // 此时 _settings 一定不为 null。添加守卫防止其他路径触发。
            if (_settings == null) return;
            _settings.IPv6AutoEnable = toggleIPv6Check.IsChecked == true;
            _settings.Save();
        }

        private void CheckIPv6_Click(object sender, RoutedEventArgs e)
        {
            var status = Services.IPv6Helper.GetIPv6StatusText();
            var enabled = Services.IPv6Helper.IsIPv6Enabled();
            var addrs = Services.IPv6Helper.GetLocalIPv6Addresses();

            var msg = $"IPv6 状态：{(enabled ? "已启用" : "未启用")}\n\n";
            msg += $"本机 IPv6 地址：\n";
            msg += addrs.Length > 0 ? string.Join("\n", addrs.Select(a => $"  {a}")) : "  无\n\n";
            msg += $"\n--- 适配器绑定状态 ---\n{status}";

            var dialog = new ConfirmDialogWindow("IPv6 状态", msg, "关闭");
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void EnableIPv6_Click(object sender, RoutedEventArgs e)
        {
            Services.IPv6Helper.EnableIPv6(this);
        }

        private void DisableIPv6_Click(object sender, RoutedEventArgs e)
        {
            Services.IPv6Helper.DisableIPv6(this);
        }

        private void Base64Encode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(txtBase64Input.Text);
                txtBase64Output.Text = Convert.ToBase64String(bytes);
                txtBase64Output.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }
            catch (Exception ex)
            {
                txtBase64Output.Text = $"编码失败: {ex.Message}";
                txtBase64Output.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
            }
        }

        private void Base64Decode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bytes = Convert.FromBase64String(txtBase64Input.Text.Trim());
                txtBase64Output.Text = System.Text.Encoding.UTF8.GetString(bytes);
                txtBase64Output.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }
            catch
            {
                txtBase64Output.Text = "解码失败：请输入有效的 Base64 字符串";
                txtBase64Output.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
            }
        }

        private void ConvertColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(txtColorInput.Text.Trim());
                colorPreview.Background = new SolidColorBrush(color);
                txtColorResult.Text = $"RGB: {color.R}, {color.G}, {color.B}  |  Hex: #{color.R:X2}{color.G:X2}{color.B:X2}";
                txtColorResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }
            catch
            {
                txtColorResult.Text = "无效的颜色值！请使用 Hex (#RRGGBB) 或已知颜色名称";
                txtColorResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
            }
        }

        // ===== 检查更新 =====
        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnCheckUpdate.IsEnabled = false;
                txtUpdateStatus.Text = "正在检查更新...";
                txtUpdateStatus.Foreground = (Brush)FindResource("TextSecondaryBrush");
                updateProgressBar.Visibility = Visibility.Collapsed;

                var updateInfo = await UpdateChecker.CheckForUpdates();

                if (updateInfo.IsUpdateAvailable)
                {
                    txtUpdateStatus.Text = $"发现新版本 {updateInfo.LatestVersion}！";
                    txtUpdateStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));

                    var result = MessageBox.Show(
                        $"发现新版本 {updateInfo.LatestVersion}！\n\n{updateInfo.ReleaseNotes}\n\n是否自动下载并安装更新？\n(选择「否」将跳转到浏览器手动下载)",
                        "更新可用",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // 自动下载安装
                        await DownloadAndInstall(updateInfo);
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // 手动下载
                        if (!string.IsNullOrEmpty(updateInfo.DownloadUrl))
                            UpdateChecker.OpenDownloadPage(updateInfo.DownloadUrl);
                        else
                            UpdateChecker.OpenDownloadPage($"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases/latest");
                    }
                }
                else
                {
                    txtUpdateStatus.Text = "当前已是最新版本 (v" + System.Reflection.Assembly.GetExecutingAssembly()
                        .GetName()?.Version?.ToString() + ")";
                    txtUpdateStatus.Foreground = (Brush)FindResource("TextSecondaryBrush");
                    ShowToast("已是最新版本");
                }
            }
            catch (Exception ex)
            {
                txtUpdateStatus.Text = "检查更新失败，请稍后重试";
                txtUpdateStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                System.Diagnostics.Debug.WriteLine($"CheckUpdate 异常: {ex.Message}");
            }
            finally
            {
                btnCheckUpdate.IsEnabled = true;
            }
        }

        private async Task DownloadAndInstall(UpdateInfo updateInfo)
        {
            try
            {
                btnCheckUpdate.IsEnabled = false;
                txtUpdateStatus.Text = "正在下载更新...";
                updateProgressBar.Visibility = Visibility.Visible;
                updateProgressBar.Value = 0;

                var progress = new Progress<int>(p =>
                {
                    updateProgressBar.Value = p;
                    txtUpdateStatus.Text = $"正在下载更新... {p}%";
                });

                string? zipPath = await UpdateChecker.DownloadUpdateAsync(updateInfo.DownloadUrl, progress);

                if (zipPath == null)
                {
                    txtUpdateStatus.Text = "下载失败，请稍后重试";
                    txtUpdateStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                    updateProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }

                txtUpdateStatus.Text = "下载完成，正在安装更新...";
                updateProgressBar.Value = 100;

                // 延时让用户看到进度到 100%
                await Task.Delay(500);

                // 安装更新（会退出程序）
                UpdateChecker.InstallUpdate(zipPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DownloadAndInstall 异常: {ex.Message}");
                MessageBox.Show($"自动安装失败：{ex.Message}\n\n请尝试手动下载更新。", "安装失败",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                updateProgressBar.Visibility = Visibility.Collapsed;
            }
            finally
            {
                btnCheckUpdate.IsEnabled = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void ShowToast(string message)
        {
            try
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
                    Opacity = 0
                };

                toast.Child = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                    FontSize = 14
                };

                // 查找可用的 Grid 容器——通过 visual tree 寻找最外层的 Grid
                Grid? grid = this.Content as Grid;
                if (grid == null && this.Content is FrameworkElement fe)
                {
                    // 如果 Content 不是 Grid（如被模板封装），尝试从模板树中找
                    var decorator = FindChild<System.Windows.Documents.AdornerDecorator>(this);
                    if (decorator != null)
                        grid = decorator.Parent as Grid;
                }
                if (grid == null)
                {
                    // 最终的 fallback：从窗口的可视化树中找 Grid
                    grid = FindChild<Grid>(this);
                }

                if (grid != null)
                {
                    toast.SetValue(Grid.ColumnSpanProperty, 2);
                    toast.SetValue(Grid.RowSpanProperty, 2);
                    grid.Children.Add(toast);

                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                    toast.BeginAnimation(OpacityProperty, fadeIn);

                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };
                    timer.Tick += (s, _) =>
                    {
                        timer.Stop();
                        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                        fadeOut.Completed += (s2, _) =>
                        {
                            if (grid != null) grid.Children.Remove(toast);
                        };
                        toast.BeginAnimation(OpacityProperty, fadeOut);
                    };
                    timer.Start();
                }
            }
            catch
            {
                // 通知显示失败不影响核心功能
            }
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
    }
}
