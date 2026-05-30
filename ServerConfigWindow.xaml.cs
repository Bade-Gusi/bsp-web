using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2;

public partial class ServerConfigWindow : Window
{
    public ServerConfigWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

            var settings = AppSettings.Load();
            if (!string.IsNullOrEmpty(settings.ServerUrl))
            {
                var scheme = settings.UseHttps ? "https" : "http";
                txtServerAddress.Text = $"{settings.ServerUrl}:{settings.ServerPort}";
                chkUseHttps.IsChecked = settings.UseHttps;
            }
        };
        this.MouseLeftButtonDown += (s, e) =>
        {
            if (e.GetPosition(this).Y < 56) this.DragMove();
        };
    }

    private (string host, int port) ParseAddress()
    {
        var addr = txtServerAddress.Text.Trim();
        if (string.IsNullOrWhiteSpace(addr))
            return ("", 0);

        // 去掉协议前缀（如果用户输入了完整的 URL）
        if (addr.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            addr = addr.Substring(8);
        else if (addr.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            addr = addr.Substring(7);

        // 去掉末尾的路径
        int slashIdx = addr.IndexOf('/');
        if (slashIdx >= 0) addr = addr.Substring(0, slashIdx);

        string host;
        int port = 5000;

        // 检查是否是 [IPv6]:port 格式
        if (addr.StartsWith("[") && addr.Contains(']'))
        {
            var closeBracket = addr.IndexOf(']');
            host = addr.Substring(1, closeBracket - 1);
            if (addr.Length > closeBracket + 1 && addr[closeBracket + 1] == ':')
                int.TryParse(addr.Substring(closeBracket + 2), out port);
        }
        // IPv4 或域名带端口: "host:port"
        else if (addr.Contains(':') && addr.Count(c => c == ':') == 1)
        {
            var parts = addr.Split(':');
            host = parts[0];
            int.TryParse(parts[1], out port);
        }
        else
        {
            host = addr;
        }

        return (host, port);
    }

    private string FormatHost(string host)
    {
        // IPv6 地址加方括号
        if (System.Net.IPAddress.TryParse(host, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return $"[{host}]";
        return host;
    }

    private string BuildApiBaseUrl()
    {
        var (host, port) = ParseAddress();
        var https = chkUseHttps.IsChecked == true;
        var formattedHost = Services.NetworkHelper.FormatHost(host);
        return $"{(https ? "https" : "http")}://{formattedHost}:{port}";
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        var addr = txtServerAddress.Text.Trim();
        if (string.IsNullOrWhiteSpace(addr))
        {
            ShowStatus("请输入服务器地址", false);
            return;
        }

        btnTest.IsEnabled = false;
        btnTest.Content = "测试中...";
        statusPanel.Visibility = Visibility.Collapsed;

        try
        {
            var baseUrl = BuildApiBaseUrl();
            using var http = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });
            http.Timeout = TimeSpan.FromSeconds(5);

            var payload = JsonSerializer.Serialize(new { username = "test", password = "test" });
            var response = await http.PostAsync($"{baseUrl}/api/auth/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            // 返回 401 说明 API 正常工作（只是登录信息不对）
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ShowStatus("✅ 连接成功！服务器运行正常", true);
            else
                ShowStatus("⚠️ 服务器异常", false);
        }
        catch (Exception ex)
        {
            ShowStatus($"❌ 连接失败: {ex.Message}", false);
        }
        finally
        {
            btnTest.IsEnabled = true;
            btnTest.Content = "测试连接";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var (host, port) = ParseAddress();
        if (string.IsNullOrWhiteSpace(host) || port <= 0)
        {
            ShowStatus("请输入有效的服务器地址（格式: 地址:端口）", false);
            return;
        }

        var settings = AppSettings.Load();
        settings.ServerUrl = host;
        settings.ServerPort = port;
        settings.UseHttps = chkUseHttps.IsChecked == true;
        settings.IsFirstLaunch = false;
        settings.Save();

        // 统一更新所有服务地址
        App.ApplyServerConfig(host, port, chkUseHttps.IsChecked == true);

        DialogResult = true;
        Close();
    }

    private void ShowStatus(string message, bool isSuccess)
    {
        statusPanel.Visibility = Visibility.Visible;
        statusPanel.Background = isSuccess
            ? new SolidColorBrush(Color.FromArgb(30, 74, 222, 128))
            : new SolidColorBrush(Color.FromArgb(30, 251, 113, 133));
        statusPanel.BorderBrush = isSuccess
            ? new SolidColorBrush(Color.FromArgb(80, 74, 222, 128))
            : new SolidColorBrush(Color.FromArgb(80, 251, 113, 133));
        statusPanel.BorderThickness = new Thickness(1);
        txtStatus.Text = message;
        txtStatus.Foreground = isSuccess
            ? new SolidColorBrush(Color.FromRgb(74, 222, 128))
            : new SolidColorBrush(Color.FromRgb(251, 113, 133));
    }
}
