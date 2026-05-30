using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class LoginWindow : Window
    {
        private bool _isOpeningMainWindow = false;

        public LoginWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
            // 启动时置顶
            this.Topmost = true;
            this.Loaded += (s, e) => this.Topmost = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 启动粒子系统
            AnimationHelper.CreateFloatingParticles(particleCanvas, count: 16,
                minSize: 2, maxSize: 4,
                colors: new[] { "#4ADE80", "#2DD4BF", "#A78BFA", "#60A5FA" },
                durationMin: 8, durationMax: 18);

            // 增强交错入场动画
            AnimateStaggeredEntrance();

            // 恢复上次输入的用户名
            var settings = AppSettings.Load();
            if (!string.IsNullOrEmpty(settings.LastUsername))
                txtUsername.Text = settings.LastUsername;
        }

        private void AnimateStaggeredEntrance()
        {
            // Logo区域：anime.js 风格弹性弹跳入场
            if (logoSection != null)
            {
                AnimationHelper.ElasticBounce(logoSection, fromScale: 0.4, overshoot: 1.12, durationSec: 0.9);
            }

            // 登录卡片：anime.js 风格交错上滑 + 弹性
            var items = new FrameworkElement[] { loginCard, steamCard, linkSection };
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                items[i].Opacity = 0;
                items[i].RenderTransformOrigin = new Point(0.5, 0.5);
                items[i].RenderTransform = new TranslateTransform(0, 40);

                int idx = i;
                double delay = 0.25 * (idx + 1);
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(delay),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var slideUp = new DoubleAnimation(40, 0, TimeSpan.FromSeconds(0.5))
                {
                    BeginTime = TimeSpan.FromSeconds(delay),
                    EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 4, EasingMode = EasingMode.EaseOut }
                };

                items[i].BeginAnimation(OpacityProperty, fadeIn);
                items[i].RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideUp);
            }

            // 背景光晕呼吸动画 — 装饰光晕在原 xaml 中无命名，此处不做 FindName
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("请输入用户名和密码");
                return;
            }

            var nameValidation = NameValidator.ValidateUsername(username);
            if (!nameValidation.IsValid)
            {
                ShowError(nameValidation.ErrorMessage);
                return;
            }

            btnLogin.IsEnabled = false;
            btnLogin.Content = "登录中...";

            var fingerprint = AntiCheatClient.GetMachineFingerprint();
            var hwInfo = GetHardwareInfo();
            var result = await ApiClient.PostAsync<AuthResponse>("/api/auth/login",
                new { username, password, machineFingerprint = fingerprint, hardwareInfo = hwInfo });

            btnLogin.IsEnabled = true;
            btnLogin.Content = "登录";

            if (result.Success && result.Data != null && result.Data.Token != null)
            {
                // 保存登录用户名
                var s = AppSettings.Load();
                s.LastUsername = username;
                s.Save();

                var token = result.Data.Token;
                var userData = result.Data.User;

                App.CurrentUser = new UserInfo
                {
                    Username = userData?.Username ?? username,
                    Token = token
                };
                App.SavedAuthToken = token;
                ApiClient.SetToken(token);

                // 使用 AuthService 保存 Token
                App.Auth.SaveToken(token, DateTime.Now.AddDays(7));

                // 登录后立即拉取完整用户信息
                var profileResult = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/auth/profile");
                if (profileResult.Success && profileResult.Data.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                {
                    var p = profileResult.Data;
                    App.CurrentUser.Username = p.TryGetProperty("username", out var u) ? u.GetString() ?? username : username;
                    // 服务端 User 模型中 avatar_url 字段
                    App.CurrentUser.Avatar = p.TryGetProperty("avatarUrl", out var av) ? av.GetString() ?? "" : "";
                    App.CurrentUser.Elo = p.TryGetProperty("mmr", out var elo) ? elo.GetInt32() : 1000;
                }

                // 连接 SignalR
                _ = App.SignalR.ConnectAsync(token);
                App.SignalR.OnServerBroadcast += OnServerBroadcast;

                var mainWindow = new MainWindow();

                var scaleAnim = new DoubleAnimation(1, 0.95, TimeSpan.FromSeconds(0.08));
                var scaleBack = new DoubleAnimation(0.95, 1, TimeSpan.FromSeconds(0.2));
                scaleBack.EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };

                btnLogin.RenderTransformOrigin = new Point(0.5, 0.5);
                btnLogin.RenderTransform = new ScaleTransform(1, 1);

                scaleAnim.Completed += (s, _) =>
                {
                    btnLogin.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleBack);
                    btnLogin.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleBack);

                    if (_isOpeningMainWindow) return;
                    _isOpeningMainWindow = true;

                    AnimationHelper.WindowCloseWithScale(this, () =>
                    {
                        mainWindow.Show();
                    });
                };

                btnLogin.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                btnLogin.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
            else
            {
                ShowError(result.Error ?? "登录失败");
            }
        }

        private void ShowError(string message)
        {
            // 错误震动动画
            var shake = new DoubleAnimation(0, 8, TimeSpan.FromSeconds(0.05));
            shake.AutoReverse = true;
            shake.RepeatBehavior = new RepeatBehavior(4);

            var translate = new TranslateTransform();
            txtUsername.RenderTransform = translate;
            translate.BeginAnimation(TranslateTransform.XProperty, shake);

            // 输入框边框闪烁红色
            if (txtUsername.IsKeyboardFocused)
            {
                // 改用 txtUsername 的直接边框替换 usernameBorder
                var border = txtUsername.Parent as Border;
                if (border != null)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 113, 133));
                    var restoreBrush = new SolidColorBrush(Color.FromRgb(45, 80, 59));
                    var colorAnim = new ColorAnimation(
                        Color.FromRgb(251, 113, 133),
                        Color.FromRgb(45, 80, 59),
                        TimeSpan.FromSeconds(1));
                    border.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                }
            }
        }

        private async void BtnSteamLogin_Click(object sender, RoutedEventArgs e)
        {
            var selector = new SteamUserSelector { Owner = this };
            if (selector.ShowDialog() != true || string.IsNullOrEmpty(selector.SelectedSteamId)) return;

            btnSteamLogin.IsEnabled = false;

            try
            {
                // 调后端 API 检查 Steam 是否已绑定
                var result = await ApiClient.PostAsync<System.Text.Json.JsonElement>("/api/auth/steam/login",
                    new { steamId = selector.SelectedSteamId });

                if (!result.Success && result.Data.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                {
                    MessageBox.Show("Steam 登录失败，请重试", "提示");
                    return;
                }

                var needRegister = result.Data.TryGetProperty("needRegister", out var nr) && nr.GetBoolean();

                if (needRegister)
                {
                    var steamId = result.Data.TryGetProperty("steamId", out var sid) ? (sid.GetString() ?? "") : "";
                    var bindWin = new SteamBindWindow { Owner = this, SteamId = steamId };
                    bindWin.ShowDialog();
                }
                else
                {
                    // 已绑定 → 解析 JWT 并登录
                    var token = result.Data.TryGetProperty("token", out var t) ? t.GetString() ?? "" : "";
                    var user = result.Data.TryGetProperty("user", out var u) ? u : default;

                    App.CurrentUser = new UserInfo
                    {
                        Username = user.TryGetProperty("username", out var un) ? un.GetString() ?? selector.SelectedUser : selector.SelectedUser,
                        SteamId = selector.SelectedSteamId,
                        Token = token
                    };
                    App.SavedAuthToken = token;
                    ApiClient.SetToken(token);
                    App.Auth.SaveToken(token, DateTime.Now.AddDays(7));

                    var avatar = SteamHelper.GetAvatar(selector.SelectedSteamId);
                    if (avatar != null)
                        App.CurrentUser.Avatar = $"steam:{selector.SelectedSteamId}";

                    _ = App.SignalR.ConnectAsync(token);

                    var mainWindow = new MainWindow();
                    AnimationHelper.WindowCloseWithScale(this, () => mainWindow.Show());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Steam 登录失败: {ex.Message}", "错误");
            }
            finally
            {
                btnSteamLogin.IsEnabled = true;
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var register = new RegisterWindow { Owner = this };
            register.Closed += (s, _) => this.Show();
            register.Show();
            this.Hide();
        }

        private void BtnForgot_Click(object sender, RoutedEventArgs e)
        {
            var forgot = new ForgotPasswordWindow { Owner = this };
            forgot.Closed += (s, _) => this.Show();
            forgot.Show();
            this.Hide();
        }

        private void BtnServerConfig_Click(object sender, RoutedEventArgs e)
        {
            var configWin = new ServerConfigWindow { Owner = this };
            if (configWin.ShowDialog() == true)
            {
                var settings = AppSettings.Load();
                var scheme = settings.UseHttps ? "https" : "http";
                ApiClient.SetBaseUrl($"{scheme}://{Services.NetworkHelper.FormatHost(settings.ServerUrl)}:{settings.ServerPort}");
                ShowToast("服务器地址已更新");
            }
        }

        private void ShowToast(string message)
        {
            if (this.Content is Panel panel)
            {
                var toast = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 74, 222, 128)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(74, 222, 128)),
                    BorderThickness = new Thickness(0, 2, 0, 0),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 10, 16, 10),
                    Margin = new Thickness(16, 16, 16, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    Opacity = 0,
                    RenderTransform = new TranslateTransform(0, -30),
                    Child = new TextBlock
                    {
                        Text = message,
                        Foreground = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                        FontSize = 13
                    }
                };
                Panel.SetZIndex(toast, 1000);
                panel.Children.Add(toast);

                toast.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                toast.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(-30, 0, TimeSpan.FromSeconds(0.35))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    toast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3)));
                    var clean = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.4) };
                    clean.Tick += (_, _) => { clean.Stop(); panel.Children.Remove(toast); };
                    clean.Start();
                };
                timer.Start();
            }
        }

        /// <summary>
        /// 收集硬件信息
        /// </summary>
        public static string GetHardwareInfo()
        {
            try
            {
                var os = Environment.OSVersion.ToString();
                var proc = Environment.Is64BitProcess ? "x64" : "x86";
                var cpu = Environment.ProcessorCount;
                var mem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                return $"OS={os}|Arch={proc}|CPU={cpu}核|MEM={mem}MB|BSP={System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
            }
            catch { return ""; }
        }

        private void ViewAgreement_Click(object sender, RoutedEventArgs e)
            => new LegalViewWindow(LegalType.Agreement) { Owner = this }.ShowDialog();

        private void ViewPrivacy_Click(object sender, RoutedEventArgs e)
            => new LegalViewWindow(LegalType.Privacy) { Owner = this }.ShowDialog();

        private void ViewDeclaration_Click(object sender, RoutedEventArgs e)
            => new LegalViewWindow(LegalType.Declaration) { Owner = this }.ShowDialog();

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var shrink = new DoubleAnimation(1, 0.85, TimeSpan.FromSeconds(0.1));
            var transform = new ScaleTransform(1, 1);
            this.RenderTransform = transform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            shrink.Completed += (_, _) =>
            {
                this.RenderTransform = null;
                this.WindowState = WindowState.Minimized;
            };
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowCloseWithScale(this);
        }

        private void OnServerBroadcast(object data)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var address = doc.RootElement.TryGetProperty("serverAddress", out var a) ? a.GetString() ?? "" : data.ToString() ?? "";
                var admin = doc.RootElement.TryGetProperty("adminName", out var n) ? n.GetString() ?? "" : "管理员";
                var popup = new ServerBroadcastPopup(address, admin, DateTime.Now);
                popup.Show();
            }
            catch { }
        }
    }
}
