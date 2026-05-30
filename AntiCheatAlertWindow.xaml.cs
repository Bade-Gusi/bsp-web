using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class AntiCheatAlertWindow : Window
    {
        public AntiCheatAlertWindow(string violation, string detail, string errorCode)
        {
            InitializeComponent();
            txtViolation.Text = violation;
            txtDetail.Text = detail;
            txtErrorCode.Text = $"错误码: {errorCode}";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 稳定淡入，不闪烁
            this.Opacity = 0;
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
        }

        private void ViolationDetail_Click(object sender, RoutedEventArgs e)
        {
            var violation = new AntiCheatViolationWindow { Owner = this };
            violation.ShowDialog();
        }

        private void Acknowledge_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () =>
            {
                // 尝试在前台打开错误报告窗口
                var mainWin = Application.Current.MainWindow;
                if (mainWin != null)
                {
                    mainWin.Show();
                    mainWin.Activate();
                }
                this.Close();
            });
        }
    }
}
