using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class LeaderboardWindow : Window
    {
        private List<LeaderboardEntry> _allEntries = new();
        private string _currentSort = "rating";
        private Button? _activeTab;

        public LeaderboardWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35)));
                HighlightTab(tabRating);
                await LoadData();
            };
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 56) DragMove();
            };
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/leaderboard?size=50");
                if (result.Success && result.Data != null)
                {
                    _allEntries = result.Data.Select((item, idx) => new LeaderboardEntry
                    {
                        Rank = idx + 1,
                        Name = (item.TryGetProperty("nickname", out var nn) ? nn.GetString() : null)
                             ?? (item.TryGetProperty("username", out var un) ? un.GetString() ?? "未知" : "未知"),
                        Score = item.TryGetProperty("mmr", out var mmr) ? mmr.GetInt32() : 0,
                        WinRate = item.TryGetProperty("totalGames", out var tg) && tg.GetInt32() > 0
                            ? (int)(item.TryGetProperty("winCount", out var wc) ? wc.GetInt32() * 100.0 / tg.GetInt32() : 0)
                            : 0,
                        TotalGames = item.TryGetProperty("totalGames", out var g) ? g.GetInt32() : 0,
                        Kills = item.TryGetProperty("killCount", out var kc) ? kc.GetInt32() : 0,
                    }).ToList();

                    UpdatePodium();
                    SortAndDisplay();
                    return;
                }
            }
            catch { }

            _allEntries = new List<LeaderboardEntry> { new LeaderboardEntry { Rank = 1, Name = "暂无数据", Score = 0 } };
            RenderList(_allEntries);
        }

        private void UpdatePodium()
        {
            var top3 = _allEntries.Take(3).ToList();
            AnimatePodium(podium1, 0.3);
            AnimatePodium(podium2, 0.15);
            AnimatePodium(podium3, 0.45);
            if (top3.Count > 0) { txtName1.Text = top3[0].Name; txtScore1.Text = FormatScore(top3[0].Score); }
            if (top3.Count > 1) { txtName2.Text = top3[1].Name; txtScore2.Text = FormatScore(top3[1].Score); }
            if (top3.Count > 2) { txtName3.Text = top3[2].Name; txtScore3.Text = FormatScore(top3[2].Score); }
        }

        private static void AnimatePodium(StackPanel panel, double delay)
        {
            panel.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5))
            { BeginTime = TimeSpan.FromSeconds(delay), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var scaleUp = new DoubleAnimation(0.5, 1, TimeSpan.FromSeconds(0.6))
            { BeginTime = TimeSpan.FromSeconds(delay), EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 5, EasingMode = EasingMode.EaseOut } };
            panel.BeginAnimation(OpacityProperty, fadeIn);
            panel.RenderTransform = new ScaleTransform(0.5, 0.5);
            panel.RenderTransformOrigin = new Point(0.5, 0.5);
            panel.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            panel.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
        }

        private void SortAndDisplay()
        {
            var sorted = _currentSort switch
            {
                "winrate" => _allEntries.OrderByDescending(e => e.WinRate).ToList(),
                "kills" => _allEntries.OrderByDescending(e => e.Kills).ToList(),
                "matches" => _allEntries.OrderByDescending(e => e.TotalGames).ToList(),
                _ => _allEntries.OrderByDescending(e => e.Score).ToList(),
            };

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].Rank = i + 1;

            RenderList(sorted);
        }

        private void RenderList(List<LeaderboardEntry> entries)
        {
            leaderboardList.Children.Clear();
            foreach (var entry in entries)
            {
                var card = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1A)),
                    BorderBrush = entry.Rank <= 3
                        ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                        : new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x2A)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 4),
                    Opacity = 0
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

                var rankColor = entry.Rank switch
                {
                    1 => "#FFD700", 2 => "#C0C0C0", 3 => "#CD7F32", _ => "#64748B"
                };

                grid.Children.Add(new TextBlock
                {
                    Text = $"#{entry.Rank}", FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(rankColor)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                grid.Children.Add(new TextBlock
                {
                    Text = entry.Name, FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0)
                });
                Grid.SetColumn(grid.Children[^1], 1);

                grid.Children.Add(new TextBlock
                {
                    Text = entry.Score.ToString(), FontSize = 14, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(grid.Children[^1], 2);

                grid.Children.Add(new TextBlock
                {
                    Text = $"{entry.WinRate}%", FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(grid.Children[^1], 3);

                grid.Children.Add(new TextBlock
                {
                    Text = entry.KillsDisplay, FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(grid.Children[^1], 4);

                card.Child = grid;
                leaderboardList.Children.Add(card);

                // 入场动画
                var delay = TimeSpan.FromSeconds(0.02 * entry.Rank);
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                { BeginTime = delay, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                card.BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        private static string FormatScore(int score) => score >= 1000 ? $"{score / 1000}.{score % 1000 / 100}k" : score.ToString();

        private void TabRating_Click(object sender, RoutedEventArgs e)
        {
            _currentSort = "rating"; SortAndDisplay(); HighlightTab(tabRating);
        }
        private void TabWinRate_Click(object sender, RoutedEventArgs e)
        {
            _currentSort = "winrate"; SortAndDisplay(); HighlightTab(tabWinRate);
        }
        private void TabKills_Click(object sender, RoutedEventArgs e)
        {
            _currentSort = "kills"; SortAndDisplay(); HighlightTab(tabKills);
        }
        private void TabMatches_Click(object sender, RoutedEventArgs e)
        {
            _currentSort = "matches"; SortAndDisplay(); HighlightTab(tabMatches);
        }

        private void HighlightTab(Button active)
        {
            if (_activeTab != null)
                _activeTab.Style = (Style)FindResource("BtnGhost");
            active.Style = (Style)FindResource("BtnPrimary");
            _activeTab = active;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => _ = LoadData();

        private void Close_Click(object sender, RoutedEventArgs e) => AnimationHelper.WindowExit(this, () => Close());
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string Name { get; set; } = "";
        public int Score { get; set; }
        public int WinRate { get; set; }
        public int TotalGames { get; set; }
        public int Kills { get; set; }
        public string KillsDisplay => Kills >= 1000 ? $"{Kills / 1000}k" : Kills.ToString();
    }
}
