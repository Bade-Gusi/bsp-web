using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class LoginHistoryWindow : Window
    {
        public ObservableCollection<LoginRecord> Records { get; set; } = new();

        public LoginHistoryWindow()
        {
            InitializeComponent();
            dgLoginHistory.ItemsSource = Records;

            Loaded += async (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                await LoadHistory();
            };
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/auth/login-history");
                if (result.Success && result.Data != null)
                {
                    Records.Clear();
                    foreach (var item in result.Data)
                    {
                        Records.Add(new LoginRecord
                        {
                            Time = item.TryGetProperty("time", out var t) ? t.GetString() ?? "" : "",
                            Device = item.TryGetProperty("device", out var d) ? d.GetString() ?? "" : "",
                            IP = item.TryGetProperty("ip", out var ip) ? ip.GetString() ?? "" : "",
                            Location = item.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "",
                        });
                    }
                    return;
                }
            }
            catch { }

            // fallback
            Records.Clear();
            Records.Add(new LoginRecord { Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), Device = "Windows 客户端", IP = "当前设备", Location = "中国" });
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    public class LoginRecord
    {
        public string Time { get; set; } = "";
        public string Device { get; set; } = "";
        public string IP { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
