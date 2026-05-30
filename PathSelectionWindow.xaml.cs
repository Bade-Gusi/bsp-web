using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class PathSelectionWindow : Window
    {
        public string SelectedPath { get; private set; } = "";

        public PathSelectionWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CS2 executable|cs2.exe|All files|*.*",
                Title = "选择 CS2 可执行文件"
            };
            if (dialog.ShowDialog() == true)
            {
                txtPath.Text = dialog.FileName;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            SelectedPath = txtPath.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
