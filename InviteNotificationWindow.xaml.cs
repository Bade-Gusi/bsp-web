using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class InviteNotificationWindow : Window
    {
        public bool Accepted { get; private set; }

        public InviteNotificationWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            this.Close();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            this.Close();
        }
    }
}
