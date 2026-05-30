using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class ConfirmExitWindow : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmExitWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 6);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25)));
            };
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
