using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class BroadcastServerWindow : Window
    {
        private const string AdminPassword = "beishui888";

        public BroadcastServerWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                txtPassword.Focus();
            };
        }

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";
            if (txtPassword.Password == AdminPassword)
            {
                panelServer.Visibility = Visibility.Visible;
                lblPassword.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Collapsed;
                txtServerAddress.Focus();
                txtServerAddress.SelectAll();
            }
        }

        private async void BtnBroadcast_Click(object sender, RoutedEventArgs e)
        {
            string address = txtServerAddress.Text.Trim();
            if (string.IsNullOrEmpty(address))
            {
                txtStatus.Text = "请输入服务器地址";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            btnBroadcast.IsEnabled = false;
            btnBroadcast.Content = "广播中...";
            txtStatus.Text = "";

            try
            {
                var result = await ApiClient.PostAsync<object>("/api/admin/broadcast", new { serverAddress = address });
                if (result.Success)
                {
                    txtStatus.Text = "✅ 广播成功！所有在线用户已收到";
                    txtStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
                }
                else
                {
                    txtStatus.Text = $"❌ 广播失败: {result.Error}";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"❌ 网络错误: {ex.Message}";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                btnBroadcast.IsEnabled = true;
                btnBroadcast.Content = "广播给所有在线用户";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.25));
            fadeOut.Completed += (_, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
