using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class AntiCheatBlockedWindow : Window
    {
        public AntiCheatBlockedWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
