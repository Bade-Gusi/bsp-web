using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class BroadcastServerWindow : Window
    {
        private const string AdminPassword = "beishui888";
        private const int CooldownMs = 3000;
        private DateTime _lastBroadcast = DateTime.MinValue;

        public BroadcastServerWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                txtPassword.Focus();
                LoadHistory();
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
            // 防抖冷却
            var elapsed = (DateTime.Now - _lastBroadcast).TotalMilliseconds;
            if (elapsed < CooldownMs)
            {
                txtStatus.Text = $"请等待 {Math.Ceiling((CooldownMs - elapsed) / 1000)} 秒";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }

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
                var result = await ApiClient.PostAsync<object>("/api/admin/broadcast",
                    new { serverAddress = address, password = AdminPassword });
                if (result.Success)
                {
                    _lastBroadcast = DateTime.Now;
                    txtStatus.Text = "✓ 广播成功！所有在线用户已收到";
                    txtStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
                    SaveHistory(address);
                }
                else
                {
                    txtStatus.Text = $"广播失败: {result.Error}";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"网络错误: {ex.Message}";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                btnBroadcast.IsEnabled = true;
                btnBroadcast.Content = "广播给所有在线用户";
                // 冷却结束后自动清除状态
                _ = System.Threading.Tasks.Task.Delay(CooldownMs).ContinueWith(_ =>
                    Dispatcher.Invoke(() => { if (txtStatus.Text.StartsWith("请等待")) txtStatus.Text = ""; }));
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/admin/latest-broadcast");
                if (result.Success && result.Data.TryGetProperty("serverAddress", out var addr))
                {
                    string address = addr.GetString() ?? "";
                    txtRefreshResult.Text = $"最新广播: {address}";
                    txtRefreshResult.Foreground = System.Windows.Media.Brushes.LimeGreen;
                }
                else
                {
                    txtRefreshResult.Text = "暂无广播记录";
                    txtRefreshResult.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
            catch
            {
                txtRefreshResult.Text = "获取失败";
                txtRefreshResult.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void SaveHistory(string address)
        {
            try
            {
                var settings = AppSettings.Load();
                var history = settings.BroadcastHistory ?? "";
                var entries = history.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (entries.Length >= 10) Array.Resize(ref entries, 9);
                settings.BroadcastHistory = address + "|" + string.Join("|", entries);
                settings.Save();
            }
            catch { }
        }

        private void LoadHistory()
        {
            try
            {
                var settings = AppSettings.Load();
                if (!string.IsNullOrEmpty(settings.BroadcastHistory))
                {
                    var entries = settings.BroadcastHistory.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    txtHistory.Text = string.Join("\n", entries);
                }
            }
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.25));
            fadeOut.Completed += (_, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
