using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class ForgotPasswordWindow : Window
    {
        private DispatcherTimer? _codeTimer;
        private int _countdown = 60;
        private int _currentStep = 1;

        public ForgotPasswordWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };

            this.Loaded += (s, e) => AnimateEntrance();
        }

        private void AnimateEntrance()
        {
            if (!(this.Content is Grid rootGrid)) return;

            // 查找内层 Grid（有 RowDefinitions 的那个）
            Grid? innerGrid = null;
            foreach (var child in rootGrid.Children)
            {
                if (child is Grid g && g.RowDefinitions.Count > 0)
                {
                    innerGrid = g;
                    break;
                }
            }

            if (innerGrid == null) return;

            var items = new[] {
                GetRowElement(innerGrid, 0), // 标题区（含关闭按钮）
                GetRowElement(innerGrid, 1), // 步骤指示器
                GetRowElement(innerGrid, 2), // 表单内容
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

        private static FrameworkElement? GetRowElement(Grid grid, int row)
        {
            foreach (var child in grid.Children)
            {
                if (child is FrameworkElement fe && Grid.GetRow(fe) == row)
                    return fe;
            }
            return null;
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == 1)
            {
                // 步骤1：验证邮箱验证码
                string email = txtEmail.Text.Trim();
                string code = txtCode.Text.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowError("请输入邮箱地址"); return;
                }
                if (!Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
                {
                    ShowError("请输入有效的邮箱地址"); return;
                }
                if (string.IsNullOrWhiteSpace(code))
                {
                    ShowError("请输入验证码"); return;
                }

                // 模拟验证（验证码 = 123456）
                if (code == "123456")
                {
                    _currentStep = 2;
                    UpdateStepUI();
                }
                else
                {
                    ShowError("验证码错误，请重试");
                }
            }
            else if (_currentStep == 2)
            {
                // 步骤2：设置新密码
                string newPwd = txtNewPassword.Password;
                string confirm = txtConfirmPassword.Password;

                if (string.IsNullOrWhiteSpace(newPwd))
                {
                    ShowError("请输入新密码"); return;
                }
                if (newPwd.Length < 6)
                {
                    ShowError("密码长度不能少于 6 位"); return;
                }
                if (newPwd != confirm)
                {
                    ShowError("两次输入的密码不一致"); return;
                }

                btnSubmit.IsEnabled = false;
                btnSubmit.Content = "处理中...";

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (s, _) =>
                {
                    timer.Stop();
                    _currentStep = 3;
                    UpdateStepUI();
                    btnSubmit.Content = "完成";
                    btnSubmit.IsEnabled = true;
                };
                timer.Start();
            }
            else
            {
                // 步骤3：完成
                if (this.Owner is Window owner)
                    owner.Show();
                else
                    new LoginWindow().Show();
                this.Close();
            }
        }

        private void UpdateStepUI()
        {
            var activeColor = (Color)ColorConverter.ConvertFromString("#4ADE80");
            var inactiveColor = (Color)ColorConverter.ConvertFromString("#2D4A35");
            var inactiveText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            var activeText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A"));

            if (_currentStep >= 1)
            {
                step1Dot.Background = new SolidColorBrush(activeColor);
            }
            if (_currentStep >= 2)
            {
                step2Dot.Background = new SolidColorBrush(activeColor);
                step2Panel.Visibility = Visibility.Visible;
                btnSubmit.Content = "确认修改";
            }
            if (_currentStep >= 3)
            {
                step3Dot.Background = new SolidColorBrush(activeColor);
                btnSubmit.Content = "返回登录";
                step1Panel.IsEnabled = false;
            }

            txtError.Visibility = Visibility.Collapsed;
        }

        private void SendCode_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("请先输入邮箱地址"); return;
            }
            if (!Regex.IsMatch(email, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                ShowError("请输入有效的邮箱地址"); return;
            }

            btnSendCode.IsEnabled = false;
            _countdown = 60;
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

            MessageBox.Show($"验证码已发送至 {email}\n（开发模式：验证码为 123456）",
                "发送成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GoLogin_Click(object sender, RoutedEventArgs e)
        {
            if (this.Owner is Window owner)
                owner.Show();
            else
                new LoginWindow().Show();
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void ShowError(string message)
        {
            txtError.Text = "⚠ " + message;
            txtError.Visibility = Visibility.Visible;

            var shake = new DoubleAnimation(0, 8, TimeSpan.FromSeconds(0.05))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            var translate = new TranslateTransform();
            txtError.RenderTransform = translate;
            translate.BeginAnimation(TranslateTransform.XProperty, shake);

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
