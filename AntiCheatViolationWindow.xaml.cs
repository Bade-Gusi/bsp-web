using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class AntiCheatViolationWindow : Window
    {
        public AntiCheatViolationWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            };
            txtDetectionTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void Appeal_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("申诉已提交，我们将尽快处理", "提示");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
