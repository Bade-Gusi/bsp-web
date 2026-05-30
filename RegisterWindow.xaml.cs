using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class RegisterWindow : Window
    {
        private DispatcherTimer? _codeTimer;
        private int _countdown = 60;

        /// <summary>外部传入的 SteamId，自动填充</summary>
        public string? PrefillSteamId { get; set; }

        public RegisterWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };

            this.Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 12);
                if (!string.IsNullOrEmpty(PrefillSteamId))
                    txtSteamId.Text = PrefillSteamId;
                AnimateEntrance();
            };
        }

        private void AnimateEntrance()
        {
            // 按 Grid.Row 顺序动画各个区域
            var items = new[] {
                GetRowElement(0), // 标题区（含关闭按钮）
                GetRowElement(1), // 表单内容区
            }.Where(e => e != null).Cast<FrameworkElement>().ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                items[i].Opacity = 0;
                items[i].RenderTransform = new TranslateTransform(0, 25);

                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(0.08 * i),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnim = new DoubleAnimation(25, 0, TimeSpan.FromSeconds(0.5))
                {
                    BeginTime = TimeSpan.FromSeconds(0.08 * i),
                    EasingFunction = new BackEase { Amplitude = 0.15, EasingMode = EasingMode.EaseOut }
                };

                items[i].BeginAnimation(OpacityProperty, opacityAnim);
                items[i].RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
            }
        }

        private FrameworkElement? GetRowElement(int row)
        {
            if (this.Content is Grid rootGrid)
            {
                foreach (var child in rootGrid.Children)
                {
                    if (child is FrameworkElement fe)
                    {
                        int r = Grid.GetRow(fe);
                        if (r == row) return fe;
                    }
                }
            }
            return null;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string confirm = txtConfirmPassword.Password;
            string code = txtCode.Text.Trim();

            // 验证输入 — 集成敏感词过滤
            var validation = NameValidator.ValidateUsername(username);
            if (!validation.IsValid)
            {
                ShowError(validation.ErrorMessage);
                return;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("请输入邮箱地址");
                return;
            }
            if (!Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                ShowError("请输入有效的邮箱地址");
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("请输入密码");
                return;
            }
            if (password.Length < 6)
            {
                ShowError("密码长度不能少于 6 位");
                return;
            }
            if (password != confirm)
            {
                ShowError("两次输入的密码不一致");
                return;
            }
            var phone = txtPhone.Text.Trim();
            var steamId = txtSteamId.Text.Trim();

            // 手机号校验
            if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^1[3-9]\d{9}$"))
            {
                ShowError("手机号格式不正确（11位手机号）");
                return;
            }
            // SteamId 校验
            if (!string.IsNullOrEmpty(steamId) && !System.Text.RegularExpressions.Regex.IsMatch(steamId, @"^\d{17}$"))
            {
                ShowError("Steam ID 格式不正确（17位数字）");
                return;
            }
            if (chkAgree.IsChecked != true)
            {
                ShowError("请阅读并同意用户协议和隐私政策");
                return;
            }
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("请输入验证码");
                return;
            }

            // 显示加载状态
            btnRegister.IsEnabled = false;
            btnRegister.Content = "注册中...";

            var fingerprint = AntiCheatClient.GetMachineFingerprint();
            var hwInfo = LoginWindow.GetHardwareInfo();
            var result = await ApiClient.PostAsync<object>("/api/auth/register", new
            {
                username = username,
                password = password,
                nickname = username,
                phone = string.IsNullOrEmpty(phone) ? null : phone,
                steamId = string.IsNullOrEmpty(steamId) ? null : steamId,
                machineFingerprint = fingerprint,
                hardwareInfo = hwInfo
            });

            btnRegister.IsEnabled = true;
            btnRegister.Content = "创建账号";

            if (result.Success)
            {
                // 注册成功，打开登录窗口
                this.Hide();
                var loginWin = new LoginWindow();
                loginWin.Show();
                this.Close();
            }
            else
            {
                ShowError(result.Error ?? "注册失败");
            }
        }

        private void SendCode_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("请先输入邮箱地址");
                return;
            }
            if (!Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                ShowError("请输入有效的邮箱地址");
                return;
            }

            btnSendCode.IsEnabled = false;
            _countdown = 60;

            // 模拟发送验证码
            _codeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _codeTimer.Tick += (s, _) =>
            {
                _countdown--;
                btnSendCode.Content = $"{_countdown}秒后重发";
                if (_countdown <= 0)
                {
                    _codeTimer?.Stop();
                    btnSendCode.Content = "发送验证码";
                    btnSendCode.IsEnabled = true;
                }
            };
            _codeTimer.Start();
            btnSendCode.Content = "60秒后重发";

            // 模拟成功提示
            var tip = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            tip.Tick += (s, _) =>
            {
                tip.Stop();
                MessageBox.Show($"验证码已发送至 {email}\n（开发模式：验证码为 123456）",
                    "发送成功", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            tip.Start();
        }

        private void GoLogin_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }

        private void ViewAgreement_Click(object sender, RoutedEventArgs e)
        {
            new LegalViewWindow(LegalType.Agreement) { Owner = this }.ShowDialog();
        }

        private void ViewPrivacy_Click(object sender, RoutedEventArgs e)
        {
            new LegalViewWindow(LegalType.Privacy) { Owner = this }.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }

        private void ShowError(string message)
        {
            txtError.Text = "⚠ " + message;
            txtError.Visibility = Visibility.Visible;

            // 震动动画
            var shake = new DoubleAnimation(0, 8, TimeSpan.FromSeconds(0.05))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            var translate = new TranslateTransform();
            txtError.RenderTransform = translate;
            translate.BeginAnimation(TranslateTransform.XProperty, shake);

            // 3秒后自动隐藏
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, _) =>
            {
                timer.Stop();
                txtError.Visibility = Visibility.Collapsed;
            };
            timer.Start();
        }
    }
}
