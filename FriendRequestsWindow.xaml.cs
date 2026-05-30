using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class FriendRequestsWindow : Window
    {
        public FriendRequestsWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 6);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                await LoadRequests();
            };
        }

        private async System.Threading.Tasks.Task LoadRequests()
        {
            try
            {
                // 获取待处理的好友请求（后端未实现独立查询时，用搜索模拟）
                // 实际应由 GET /api/friends/requests 返回
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/friends/requests");
                // 如果端点不存在，静默处理
                if (result.Success && result.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        var fromName = item.TryGetProperty("username", out var un) ? un.GetString() ?? "未知" : "未知";
                        var fromId = item.TryGetProperty("fromUserId", out var fid) ? fid.GetInt32() : 0;
                        AddRequestCard(fromName, fromId);
                    }
                }
            }
            catch { }

            if (requestsPanel.Children.Count == 0)
            {
                requestsPanel.Children.Add(new TextBlock
                {
                    Text = "暂无待处理的好友请求",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
            }
        }

        private void AddRequestCard(string fromName, int fromId)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1A)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                Tag = fromId
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(new TextBlock
            {
                Text = $"{fromName} 请求添加你为好友",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });

            var acceptBtn = new Button
            {
                Content = "接受",
                Width = 70, Height = 32, FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0),
                Tag = fromId
            };
            acceptBtn.Click += AcceptRequest_Click;
            Grid.SetColumn(acceptBtn, 1);

            var rejectBtn = new Button
            {
                Content = "拒绝",
                Width = 70, Height = 32, FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                BorderThickness = new Thickness(0),
                Tag = fromId
            };
            rejectBtn.Click += RejectRequest_Click;
            Grid.SetColumn(rejectBtn, 2);

            grid.Children.Add(acceptBtn);
            grid.Children.Add(rejectBtn);
            card.Child = grid;
            requestsPanel.Children.Add(card);

            // 入场动画
            card.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            card.BeginAnimation(OpacityProperty, fadeIn);
        }

        private async void AcceptRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int friendId && friendId > 0)
            {
                var result = await ApiClient.PostAsync<object>($"/api/friends/accept/{friendId}", new { });
                if (result.Success)
                {
                    var parent = btn.Parent as Grid;
                    if (parent?.Parent is Border border)
                    {
                        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
                        fadeOut.Completed += (_, _) => requestsPanel.Children.Remove(border);
                        border.BeginAnimation(OpacityProperty, fadeOut);
                    }
                }
            }
        }

        private async void RejectRequest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int friendId && friendId > 0)
            {
                // 拒绝逻辑
                var parent = btn.Parent as Grid;
                if (parent?.Parent is Border border)
                    requestsPanel.Children.Remove(border);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => Close());
        }
    }
}
