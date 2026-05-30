using System;
using System.Windows;
using System.Windows.Media.Animation;
using BeiShuiCS2.Services;

namespace BeiShuiCS2;

public partial class CreateServerWindow : Window
{
    public CreateServerWindow()
    {
        InitializeComponent();

        sliderPlayers.ValueChanged += (s, e) =>
        {
            txtPlayerCount.Text = $"{(int)e.NewValue} 人";
        };

        Loaded += (s, e) =>
        {
            AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
        };

        MouseLeftButtonDown += (s, e) =>
        {
            if (e.GetPosition(this).Y < 56) DragMove();
        };
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        btnCreate.IsEnabled = false;
        resultPanel.Visibility = Visibility.Collapsed;
        loadingPanel.Visibility = Visibility.Visible;

        var mapItem = cmbMap.SelectedItem as System.Windows.Controls.ComboBoxItem;
        var modeItem = cmbMode.SelectedItem as System.Windows.Controls.ComboBoxItem;
        var mapName = mapItem?.Tag as string ?? "de_mirage";
        if (string.IsNullOrEmpty(mapName) || mapItem == null)
        {
            // 从显示文本提取地图名
            var text = mapItem?.Content?.ToString() ?? "de_mirage";
            var parts = text.Split(' ');
            mapName = parts.Length > 1 ? parts[^1] : "de_mirage";
        }

        if (mapName.StartsWith("de_") == false) mapName = "de_mirage";

        // 从下拉框文本提取地图
        var selectedMap = mapItem?.Content?.ToString() ?? "";
        var parts2 = selectedMap.Split(' ');
        mapName = parts2.Length > 1 ? parts2[^1] : "de_mirage";

        int mode = cmbMode.SelectedIndex; // 0=竞技 1=休闲 2=死斗
        int maxPlayers = (int)sliderPlayers.Value;
        string? password = string.IsNullOrWhiteSpace(txtPassword.Text) ? null : txtPassword.Text;

        try
        {
            var result = await ApiClient.PostAsync<CreateServerResponse>("/api/servers", new
            {
                MapName = mapName,
                Mode = mode,
                MaxPlayers = maxPlayers,
                Password = password
            });

            loadingPanel.Visibility = Visibility.Collapsed;

            if (result.Success && result.Data != null)
            {
                resultPanel.Visibility = Visibility.Visible;
                var data = result.Data;
                txtServerAddress.Text = $"服务器地址: {data.ConnectAddress}\n" +
                                        $"地图: {selectedMap}\n" +
                                        $"模式: {modeItem?.Content}\n" +
                                        $"最大人数: {maxPlayers}";

                // 自动复制
                try { Clipboard.SetText(data.ConnectAddress); } catch { }
            }
            else
            {
                MessageBox.Show($"创建失败: {result.Error ?? "未知错误"}", "提示");
            }
        }
        catch (Exception ex)
        {
            loadingPanel.Visibility = Visibility.Collapsed;
            MessageBox.Show($"创建失败: {ex.Message}", "提示");
        }
        finally
        {
            btnCreate.IsEnabled = true;
        }
    }

    private void CopyAddress_Click(object sender, RoutedEventArgs e)
    {
        var addr = txtServerAddress.Text;
        try { Clipboard.SetText(addr); } catch { }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.WindowCloseWithScale(this, () => Close());
    }

    public class CreateServerResponse
    {
        public string RoomCode { get; set; } = "";
        public string ServerIp { get; set; } = "";
        public int ServerPort { get; set; }
        public string ConnectAddress { get; set; } = "";
        public string RconPassword { get; set; } = "";
    }
}
