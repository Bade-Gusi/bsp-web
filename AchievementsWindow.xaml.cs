using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class AchievementsWindow : Window
    {
        public AchievementsWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4)));
                await LoadAchievements();
            };
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 64) DragMove();
            };
        }

        private async System.Threading.Tasks.Task LoadAchievements()
        {
            try
            {
                var profile = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/auth/profile");
                if (profile.Success && profile.Data.TryGetProperty("id", out var idProp))
                {
                    var userId = idProp.GetInt64();
                    var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>($"/api/achievements/{userId}");
                    if (result.Success && result.Data != null)
                    {
                        var list = new List<Achievement>();
                        foreach (var item in result.Data)
                        {
                            list.Add(new Achievement
                            {
                                Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                                Description = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                                Icon = item.TryGetProperty("icon", out var i) ? i.GetString() ?? "🏆" : "🏆",
                                IsUnlocked = item.TryGetProperty("unlocked", out var u) && u.GetBoolean(),
                                Progress = item.TryGetProperty("progress", out var p) ? p.GetInt32() : 0,
                                MaxProgress = item.TryGetProperty("max", out var m) ? m.GetInt32() : 1
                            });
                        }

                        int unlocked = list.FindAll(a => a.IsUnlocked).Count;
                        txtProgress.Text = $"已解锁 {unlocked} / {list.Count}";
                        txtPercent.Text = list.Count > 0 ? $"{(int)((double)unlocked / list.Count * 100)}%" : "0%";

                        RenderAchievements(list);
                        return;
                    }
                }
            }
            catch { }

            txtProgress.Text = "已解锁 0 / 0";
            txtPercent.Text = "0%";
        }

        private void RenderAchievements(List<Achievement> achievements)
        {
            achievementsPanel.Children.Clear();
            for (int i = 0; i < achievements.Count; i++)
            {
                var ach = achievements[i];
                var card = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1A)),
                    BorderBrush = ach.IsUnlocked
                        ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                        : new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 8),
                    Opacity = 0
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var iconText = new TextBlock
                {
                    Text = ach.Icon,
                    FontSize = 28,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 16, 0)
                };
                Grid.SetColumn(iconText, 0);

                var infoStack = new StackPanel();
                infoStack.Children.Add(new TextBlock
                {
                    Text = ach.Title,
                    FontSize = 16, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0))
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = ach.Description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                    Margin = new Thickness(0, 2, 0, 8)
                });

                var progressOuter = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x1A, 0x10)),
                    CornerRadius = new CornerRadius(4), Height = 6, Width = 180,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                var pct = ach.MaxProgress > 0 ? (double)ach.Progress / ach.MaxProgress : 0.0;
                progressOuter.Child = new Border
                {
                    Background = ach.IsUnlocked
                        ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                        : new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    CornerRadius = new CornerRadius(4),
                    Width = 180 * pct, HorizontalAlignment = HorizontalAlignment.Left, Height = 6
                };
                infoStack.Children.Add(progressOuter);
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"{ach.Progress} / {ach.MaxProgress}", FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    Margin = new Thickness(0, 2, 0, 0)
                });

                Grid.SetColumn(infoStack, 1);

                var statusText = new TextBlock
                {
                    Text = ach.IsUnlocked ? "✅ 已解锁" : "🔒 未解锁", FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = ach.IsUnlocked
                        ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                        : new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(statusText, 2);

                grid.Children.Add(iconText);
                grid.Children.Add(infoStack);
                grid.Children.Add(statusText);
                card.Child = grid;
                achievementsPanel.Children.Add(card);

                var delay = TimeSpan.FromSeconds(0.08 * i);
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                { BeginTime = delay, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var slideUp = new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.4))
                { BeginTime = delay, EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut } };
                card.BeginAnimation(OpacityProperty, fadeIn);
                card.RenderTransform = new TranslateTransform(0, 20);
                card.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideUp);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => Close());
        }
    }

    public class Achievement
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsUnlocked { get; set; }
        public int Progress { get; set; }
        public int MaxProgress { get; set; }
    }
}
