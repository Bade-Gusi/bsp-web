using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class DemoPlayerWindow : Window
    {
        private string? demoFilePath;

        public DemoPlayerWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && Path.GetExtension(files[0]).ToLower() == ".dem")
                {
                    LoadDemoFile(files[0]);
                }
            }
        }

        private void LoadDemoFile(string path)
        {
            demoFilePath = path;
            txtFileName.Text = Path.GetFileName(path);
            txtFileName.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8F5E9"));
            btnPlay.IsEnabled = true;

            // 文件加载动画
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            txtFileName.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(demoFilePath)) return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{demoFilePath}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);

                var mainWin = Application.Current.MainWindow as MainWindow;
                mainWin?.ShowToast("已打开Demo文件位置");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开文件: {ex.Message}", "错误");
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CS2 Demo files|*.dem|All files|*.*",
                Title = "选择 Demo 文件"
            };
            if (dialog.ShowDialog() == true)
            {
                LoadDemoFile(dialog.FileName);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
