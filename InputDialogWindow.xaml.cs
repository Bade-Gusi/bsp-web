using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class InputDialogWindow : Window
    {
        public string Result { get; private set; } = "";
        public string InputText => Result;

        public InputDialogWindow(string title, string description, string defaultValue = "")
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
            txtTitle.Text = title;
            txtDescription.Text = description;
            txtInput.Text = defaultValue;
            txtInput.SelectAll();

            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Result = txtInput.Text.Trim();
            this.DialogResult = true;
            this.Close();
        }
    }
}
