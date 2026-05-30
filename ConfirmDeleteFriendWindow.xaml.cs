using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class ConfirmDeleteFriendWindow : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmDeleteFriendWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            this.Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            this.Close();
        }
    }
}
