using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class VerifyWindow : Window
    {
        public VerifyWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
        }

        private void VerifyEmail_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("验证邮件已发送，请查收", "提示");
        }

        private void BindPhone_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("手机绑定功能开发中", "提示");
        }

        private void VerifyIdentity_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("身份认证功能开发中", "提示");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
