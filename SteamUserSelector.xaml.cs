using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class SteamUserSelector : Window
    {
        public string SelectedUser { get; private set; } = "";
        public string SelectedSteamId { get; private set; } = "";

        public SteamUserSelector()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                LoadUsers();
            };
        }

        private void LoadUsers()
        {
            var realUsers = SteamHelper.GetLoggedInUsers();

            if (realUsers.Count == 0)
            {
                var hint = new TextBlock
                {
                    Text = "未检测到 Steam 登录用户\n请确保 Steam 客户端正在运行并已登录",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                usersPanel.Children.Add(hint);
                return;
            }

            // 收集需要异步加载头像的条目
            var pendingAvatars = new List<(Border container, string steamId, string personaName)>();

            foreach (var user in realUsers)
            {
                var item = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = user
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // 头像容器（先用首字母占位，后台下载头像后替换）
                var avatarContainer = new Border
                {
                    Width = 44,
                    Height = 44,
                    CornerRadius = new CornerRadius(22),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                    ClipToBounds = true
                };

                // 尝试从本地缓存加载
                var cachedAvatar = SteamHelper.GetAvatar(user.SteamID64);
                if (cachedAvatar != null)
                {
                    SetAvatarImage(avatarContainer, cachedAvatar);
                }
                else
                {
                    // 先显示首字母
                    avatarContainer.Child = new TextBlock
                    {
                        Text = user.PersonaName.Length > 0
                            ? user.PersonaName.Substring(0, 1).ToUpper()
                            : "?",
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        FontSize = 18,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    pendingAvatars.Add((avatarContainer, user.SteamID64, user.PersonaName));
                }
                Grid.SetColumn(avatarContainer, 0);

                // 用户名 + SteamID
                var namePanel = new StackPanel
                {
                    Margin = new Thickness(12, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var nameText = new TextBlock
                {
                    Text = user.PersonaName,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var steamIdText = new TextBlock
                {
                    Text = $"SteamID: {user.SteamID64}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    FontFamily = (FontFamily)FindResource("FontMono"),
                    Margin = new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                namePanel.Children.Add(nameText);
                namePanel.Children.Add(steamIdText);
                Grid.SetColumn(namePanel, 1);

                // 箭头指示
                var arrowText = new TextBlock
                {
                    Text = "→",
                    FontSize = 20,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(arrowText, 2);

                grid.Children.Add(avatarContainer);
                grid.Children.Add(namePanel);
                grid.Children.Add(arrowText);
                item.Child = grid;

                // 点击选择
                item.MouseLeftButtonDown += (s, e) =>
                {
                    if (item.Tag is SteamUser selected)
                    {
                        SelectedUser = selected.PersonaName;
                        SelectedSteamId = selected.SteamID64;
                        DialogResult = true;
                        Close();
                    }
                };

                // hover 效果
                item.MouseEnter += (s, e) =>
                {
                    item.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A4A2A"));
                };
                item.MouseLeave += (s, e) =>
                {
                    item.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F"));
                };

                usersPanel.Children.Add(item);
            }

            // 底部提示
            var tip = new TextBlock
            {
                Text = $"检测到 {realUsers.Count} 个 Steam 账号，点击选择登录",
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            usersPanel.Children.Add(tip);

            // 后台异步加载头像（不阻塞 UI）
            if (pendingAvatars.Count > 0)
                DownloadAvatarsAsync(pendingAvatars);
        }

        private static void SetAvatarImage(Border container, BitmapImage bitmap)
        {
            var img = new Image
            {
                Source = bitmap,
                Width = 44,
                Height = 44,
                Stretch = Stretch.UniformToFill
            };
            img.Clip = new EllipseGeometry
            {
                RadiusX = 22,
                RadiusY = 22,
                Center = new Point(22, 22)
            };
            container.Child = img;
        }

        private async void DownloadAvatarsAsync(List<(Border container, string steamId, string personaName)> pending)
        {
            foreach (var (container, steamId, _) in pending)
            {
                await System.Threading.Tasks.Task.Run(async () =>
                {
                    await SteamHelper.DownloadAvatarAsync(steamId);
                }).ConfigureAwait(false);

                // 下载完成后从缓存加载并更新 UI（回到 UI 线程）
                var avatar = SteamHelper.GetAvatar(steamId);
                if (avatar != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        SetAvatarImage(container, avatar);
                    });
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
