using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class AntiCheatWarningWindow : Window
    {
        public AntiCheatWarningWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
            };
        }

        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            var verify = new VerifyWindow { Owner = this };
            verify.ShowDialog();
            AnimationHelper.WindowExit(this, () => this.Close());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
