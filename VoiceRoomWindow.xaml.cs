using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class VoiceRoomWindow : Window
    {
        public ObservableCollection<VoiceMemberInfo> Members { get; set; } = new();

        public VoiceRoomWindow()
        {
            InitializeComponent();
            DataContext = this;
            memberList.ItemsSource = Members;

            Loaded += async (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 6);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                await LoadRooms();
            };
        }

        private async System.Threading.Tasks.Task LoadRooms()
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/voice/rooms/mine");
                if (result.Success && result.Data != null)
                {
                    RenderRooms(result.Data, true);
                }

                var allResult = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/voice/rooms");
                if (allResult.Success && allResult.Data != null)
                {
                    var myCodes = result.Data?.Select(r =>
                    {
                        try { return r.GetProperty("roomCode").GetString() ?? ""; } catch { return ""; }
                    }).ToHashSet() ?? new();

                    var others = allResult.Data.Where(r =>
                    {
                        try { return !myCodes.Contains(r.GetProperty("roomCode").GetString() ?? ""); } catch { return false; }
                    }).ToArray();

                    if (others.Length > 0)
                        RenderRooms(others, false);
                }
            }
            catch { }
        }

        private void RenderRooms(System.Text.Json.JsonElement[] rooms, bool isMyRooms)
        {
            foreach (var r in rooms)
            {
                var code = r.TryGetProperty("roomCode", out var rc) ? rc.GetString() ?? "" : "";
                var name = r.TryGetProperty("name", out var rn) ? rn.GetString() ?? "" : "";
                var cur = r.TryGetProperty("currentUsers", out var cu) ? cu.GetInt32() : 0;
                var max = r.TryGetProperty("maxUsers", out var mu) ? mu.GetInt32() : 10;
                var host = r.TryGetProperty("hostName", out var hn) ? hn.GetString() ?? "" : "";

                var card = new Border
                {
                    Tag = code,
                    CornerRadius = new System.Windows.CornerRadius(8),
                    Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x1A, 0x2E, 0x1A)),
                    Padding = new System.Windows.Thickness(10),
                    Margin = new System.Windows.Thickness(0, 0, 0, 4),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var stack = new StackPanel();
                stack.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = name,
                    FontSize = 14,
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xE2, 0xE8, 0xF0))
                });
                stack.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = isMyRooms ? $"🏠 {host} · {cur}/{max}" : $"📡 {host} · {cur}/{max}",
                    FontSize = 11,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x94, 0xA3, 0xB8))
                });
                card.Child = stack;

                card.MouseLeftButtonDown += (s, args) =>
                {
                    if (s is Border b && b.Tag is string codeStr)
                        SelectRoom(codeStr, name);
                };

                roomListPanel.Children.Add(card);
            }
        }

        private async void SelectRoom(string code, string name)
        {
            txtCurrentRoom.Text = name;
            txtRoomCode.Text = $"房间码: {code}";

            // 加成员
            Members.Clear();
            Members.Add(new VoiceMemberInfo { Name = App.CurrentUser?.Username ?? "我", Initial = "我", Badge = "房主", Status = "在线" });

            // 模拟其他成员（实际应从服务器获取）
            try { Clipboard.SetText(code); } catch { }

            try { await ApiClient.PostAsync<object>($"/api/voice/rooms/{code}/join", new { }); } catch { }
        }

        private async void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox("输入语音房间名称:", "创建语音房间", $"{App.CurrentUser?.Username ?? "玩家"}的频道");
            if (string.IsNullOrWhiteSpace(name)) return;

            var result = await ApiClient.PostAsync<System.Text.Json.JsonElement>("/api/voice/rooms",
                new { name, maxUsers = 10 });

            if (result.Success)
            {
                // 刷新列表
                roomListPanel.Children.Clear();
                await LoadRooms();
            }
            else
            {
                MessageBox.Show(result.Error ?? "创建失败", "提示");
            }
        }

        private async void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            var code = Microsoft.VisualBasic.Interaction.InputBox("输入房间码:", "加入语音房间", "");
            if (string.IsNullOrWhiteSpace(code)) return;

            var result = await ApiClient.PostAsync<object>($"/api/voice/rooms/{code}/join", new { });
            if (result.Success)
            {
                // 刷新
                roomListPanel.Children.Clear();
                await LoadRooms();
            }
            else
            {
                MessageBox.Show(result.Error ?? "加入失败", "提示");
            }
        }

        private void JoinVoice_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtRoomCode.Text)) return;

            var code = txtRoomCode.Text.Replace("房间码: ", "");
            var voiceWin = new VoiceCallWindow { Owner = this };
            voiceWin.Show();
        }

        private async void LeaveRoom_Click(object sender, RoutedEventArgs e)
        {
            var code = txtRoomCode.Text.Replace("房间码: ", "");
            if (string.IsNullOrEmpty(code)) return;

            await ApiClient.PostAsync<object>($"/api/voice/rooms/{code}/leave", new { });

            Members.Clear();
            txtCurrentRoom.Text = "选择一个房间";
            txtRoomCode.Text = "";

            roomListPanel.Children.Clear();
            await LoadRooms();
        }
    }

    public class VoiceMemberInfo
    {
        public string Name { get; set; } = "";
        public string Initial { get; set; } = "?";
        public string Badge { get; set; } = "";
        public string Status { get; set; } = "在线";
    }
}
