using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2;

public partial class ServerAuthWindow : Window
{
    public ServerAuthWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

            // 加载上次保存的地址
            var settings = AppSettings.Load();
            if (!string.IsNullOrEmpty(settings.ServerUrl))
                txtServerUrl.Text = settings.ServerUrl;
            if (settings.ServerPort > 0)
                txtPort.Text = settings.ServerPort.ToString();
            chkUseHttps.IsChecked = settings.UseHttps;
        };
        this.MouseLeftButtonDown += (s, e) =>
        {
            if (e.GetPosition(this).Y < 56) this.DragMove();
        };
    }

    private string BuildBaseUrl()
    {
        var url = txtServerUrl.Text.Trim();
        var port = txtPort.Text.Trim();
        var https = chkUseHttps.IsChecked == true;
        var scheme = https ? "https" : "http";
        return $"{scheme}://{url}:{port}";
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        var url = txtServerUrl.Text.Trim();
        var port = txtPort.Text.Trim();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(port))
        {
            ShowStatus("请输入服务器地址和端口", false);
            return;
        }

        btnTest.IsEnabled = false;
        btnTest.Content = "测试中...";
        statusPanel.Visibility = Visibility.Collapsed;

        try
        {
            var baseUrl = BuildBaseUrl();
            using var http = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });
            http.Timeout = TimeSpan.FromSeconds(5);

            var payload = JsonSerializer.Serialize(new { username = "test", password = "test" });
            var response = await http.PostAsync($"{baseUrl}/api/auth/login",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
                ShowStatus("✅ 连接成功！服务器运行正常", true);
            else
                ShowStatus("⚠️ 服务器响应异常，请检查地址和端口", false);
        }
        catch (HttpRequestException)
        {
            ShowStatus("❌ 无法连接到服务器，请检查地址和端口", false);
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
        var url = txtServerUrl.Text.Trim();
        var portStr = txtPort.Text.Trim();

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(portStr))
        {
            ShowStatus("请输入服务器地址和端口", false);
            return;
        }

        if (!int.TryParse(portStr, out var port) || port < 1 || port > 65535)
        {
            ShowStatus("端口号无效（1-65535）", false);
            return;
        }

        var settings = AppSettings.Load();
        settings.ServerUrl = url;
        settings.ServerPort = port;
        settings.UseHttps = chkUseHttps.IsChecked == true;
        settings.Save();

        // 设置 API 客户端
        ApiClient.SetBaseUrl(BuildBaseUrl());

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
