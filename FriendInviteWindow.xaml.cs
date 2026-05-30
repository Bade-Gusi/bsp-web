using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BeiShuiCS2
{
    public partial class FriendInviteWindow : Window
    {
        private List<Friend> _friends = new();

        public FriendInviteWindow()
        {
            InitializeComponent();
            this.Loaded += async (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                await LoadFriends();
            };
        }

        private async System.Threading.Tasks.Task LoadFriends()
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/friends");
                if (result.Success && result.Data != null)
                {
                    _friends = result.Data.Select(f => new Friend
                    {
                        FriendId = f.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                        Name = f.TryGetProperty("nickname", out var nn) && nn.GetString() is string nname
                            ? nname
                            : (f.TryGetProperty("username", out var un) ? un.GetString() ?? "" : ""),
                        ELO = f.TryGetProperty("mmr", out var mmr) ? mmr.GetInt32() : 1000,
                        IsOnline = f.TryGetProperty("status", out var st) && st.GetInt32() == 1
                    }).ToList();
                }
            }
            catch
            {
                _friends = new List<Friend>();
            }

            RenderFriends(_friends);
        }

        private void RenderFriends(List<Friend> friends)
        {
            friendsPanel.Children.Clear();

            foreach (var friend in friends)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1A")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = friend
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var statusDot = new Ellipse
                {
                    Width = 8, Height = 8,
                    Fill = friend.IsOnline
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"))
                        : Brushes.Gray,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(statusDot, 0);

                var nameText = new TextBlock
                {
                    Text = friend.Name,
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameText, 1);

                var eloText = new TextBlock
                {
                    Text = $"MMR: {friend.ELO}",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(eloText, 2);

                grid.Children.Add(statusDot);
                grid.Children.Add(nameText);
                grid.Children.Add(eloText);
                card.Child = grid;

                card.MouseLeftButtonDown += (s, e) =>
                {
                    if (s is Border border && border.Tag is Friend)
                    {
                        var bg = border.Background as SolidColorBrush;
                        border.Background = bg != null && bg.Color == Color.FromRgb(0x1A, 0x2E, 0x1A)
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A4A2A"))
                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1A"));
                    }
                };

                friendsPanel.Children.Add(card);
            }
        }

        private void SendInvite_Click(object sender, RoutedEventArgs e)
        {
            var selected = friendsPanel.Children.OfType<Border>()
                .Where(b => b.Background is SolidColorBrush brush &&
                            brush.Color == Color.FromRgb(0x2A, 0x4A, 0x2A))
                .Select(b => b.Tag)
                .OfType<Friend>()
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("请先选择要邀请的好友", "提示");
                return;
            }

            foreach (var friend in selected)
            {
                if (App.SignalR != null && friend.FriendId > 0)
                {
                    _ = App.SignalR.SendPrivateMessage(friend.FriendId, "邀请你一起游戏");
                }

                MessageBox.Show($"已向 {friend.Name} 发送游戏邀请！", "邀请已发送",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = txtSearch.Text.ToLower();
            var filtered = _friends.Where(f =>
                f.Name.ToLower().Contains(query)).ToList();
            RenderFriends(filtered);
        }
    }
}
