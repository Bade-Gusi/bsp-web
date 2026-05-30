using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class ServerBroadcastPopup : Window
    {
        public ServerBroadcastPopup(string serverAddress, string adminName, DateTime timestamp)
        {
            InitializeComponent();

            txtServerAddress.Text = serverAddress;
            txtInfo.Text = $"来自: {adminName} · {timestamp:HH:mm:ss}";

            this.Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35)));
                var bounce = new DoubleAnimation(0.8, 1, TimeSpan.FromSeconds(0.4));
                bounce.EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };
                (this.Content as UIElement).RenderTransformOrigin = new Point(0.5, 0.5);
                (this.Content as UIElement).RenderTransform = new ScaleTransform(0.8, 0.8);
                (this.Content as UIElement).RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, bounce);
                (this.Content as UIElement).RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, bounce);
            };
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string address = txtServerAddress.Text.Trim();
            if (!string.IsNullOrEmpty(address))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"steam://connect/{address}",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
            CloseWithAnimation();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtServerAddress.Text);
                btnCopy.Content = "已复制";
            }
            catch { }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWithAnimation();
        }

        private void CloseWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.25));
            fadeOut.Completed += (_, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
