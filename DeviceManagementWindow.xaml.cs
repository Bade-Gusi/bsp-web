using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class DeviceManagementWindow : Window
    {
        public DeviceManagementWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 窗口淡入
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

            // 粒子背景
            AnimationHelper.CreateFloatingParticles(particleCanvas, 8);

            // 加载设备数据
            LoadDevices();
        }

        private void LoadDevices()
        {
            var devices = new[]
            {
                new DeviceInfo { Name = "Windows 台式机", Type = "PC", Location = "北京市",
                                 LastLogin = DateTime.Now.AddMinutes(-5), IsCurrent = true, IsOnline = true },
                new DeviceInfo { Name = "MacBook Pro", Type = "Mac", Location = "上海市",
                                 LastLogin = DateTime.Now.AddDays(-3), IsCurrent = false, IsOnline = false },
                new DeviceInfo { Name = "OnePlus 12", Type = "Android", Location = "广州市",
                                 LastLogin = DateTime.Now.AddDays(-15), IsCurrent = false, IsOnline = false },
                new DeviceInfo { Name = "iPhone 15", Type = "iOS", Location = "深圳市",
                                 LastLogin = DateTime.Now.AddDays(-7), IsCurrent = false, IsOnline = false },
                new DeviceInfo { Name = "Windows 笔记本", Type = "PC", Location = "杭州市",
                                 LastLogin = DateTime.Now.AddDays(-30), IsCurrent = false, IsOnline = false },
            };

            txtDeviceCount.Text = $"共 {devices.Length} 台设备";

            int delay = 0;
            foreach (var device in devices)
            {
                var card = CreateDeviceCard(device, delay);
                deviceListPanel.Children.Add(card);
                delay++;
            }
        }

        private Border CreateDeviceCard(DeviceInfo device, int index)
        {
            string icon = device.Type switch
            {
                "PC" => "\uE7A5",       // DesktopWindows
                "Mac" => "\uE77C",      // Laptop
                "Android" => "\uE8EA",  // PhoneAndroid
                "iOS" => "\uE8EA",      // PhoneIphone
                _ => "\uE7A5"
            };

            Color accentColor = device.IsOnline ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0x6B, 0x72, 0x80);

            // 设备卡片
            var card = new Border
            {
                Background = (Brush)FindResource("BgCardBrush"),
                BorderBrush = (Brush)FindResource("BorderPrimaryBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12),
                Opacity = 0,
                RenderTransform = new TranslateTransform(0, 20),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 设备图标
            var iconBorder = new Border
            {
                Width = 44, Height = 44,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(20, accentColor.R, accentColor.G, accentColor.B)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            var iconText = new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 22,
                Foreground = new SolidColorBrush(accentColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // 设备信息
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(infoStack, 1);

            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            var nameText = new TextBlock
            {
                Text = device.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            nameRow.Children.Add(nameText);

            if (device.IsCurrent)
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 0x4A, 0xDE, 0x80)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var badgeText = new TextBlock
                {
                    Text = "当前设备",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                };
                badge.Child = badgeText;
                nameRow.Children.Add(badge);
            }
            infoStack.Children.Add(nameRow);

            // 位置 + 时间
            var detailText = new TextBlock
            {
                Text = $"{device.Location}  ·  上次登录: {device.LastLogin:MM-dd HH:mm}",
                FontSize = 12,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            infoStack.Children.Add(detailText);

            grid.Children.Add(infoStack);

            // 操作按钮
            var actionStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 0, 0, 0)
            };
            Grid.SetColumn(actionStack, 2);

            if (!device.IsCurrent)
            {
                var btnKick = new Button
                {
                    Content = "下线",
                    Style = (Style)FindResource("BtnGhost"),
                    MinWidth = 70,
                    Height = 34,
                    FontSize = 12,
                    Tag = device
                };
                btnKick.Click += KickDevice_Click;
                actionStack.Children.Add(btnKick);
            }

            grid.Children.Add(actionStack);
            card.Child = grid;

            // 交错入场动画
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
            {
                BeginTime = TimeSpan.FromSeconds(0.1 + index * 0.08),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            card.BeginAnimation(OpacityProperty, fadeIn);

            var slideIn = new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.4))
            {
                BeginTime = TimeSpan.FromSeconds(0.1 + index * 0.08),
                EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
            };
            card.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);

            return card;
        }

        private void KickDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DeviceInfo device)
            {
                var result = MessageBox.Show(
                    $"确定要将设备 \"{device.Name}\" 下线吗？\n该设备将被立即登出账号。",
                    "确认下线", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // 移除卡片动画
                    var card = btn.Parent;
                    while (card != null && card is not Border)
                        card = LogicalTreeHelper.GetParent(card as DependencyObject);
                    if (card is Border border)
                    {
                        border.BeginAnimation(OpacityProperty,
                            new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.25)));
                        border.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                            new DoubleAnimation(0, -20, TimeSpan.FromSeconds(0.25)));

                        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                        timer.Tick += (_, _) =>
                        {
                            timer.Stop();
                            deviceListPanel.Children.Remove(border);
                            UpdateDeviceCount();
                            ShowToast($"{device.Name} 已下线");
                        };
                        timer.Start();
                    }
                }
            }
        }

        private void UpdateDeviceCount()
        {
            int count = deviceListPanel.Children.Count;
            txtDeviceCount.Text = $"共 {count} 台设备";
        }

        private void ShowToast(string message)
        {
            var toast = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1F)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 14, 20, 14),
                Margin = new Thickness(0, 0, 24, 24),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Opacity = 0,
                RenderTransform = new TranslateTransform(100, 0)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9)),
                FontSize = 14
            };
            toast.Child = textBlock;

            var root = this.Content as Grid;
            if (root != null)
            {
                root.Children.Add(toast);
                toast.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
                toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(100, 0, TimeSpan.FromSeconds(0.4))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    var opacityOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                    opacityOut.Completed += (_, _) => root.Children.Remove(toast);
                    toast.BeginAnimation(OpacityProperty, opacityOut);
                    toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                        new DoubleAnimation(0, 100, TimeSpan.FromSeconds(0.3)));
                };
                timer.Start();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class DeviceInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Location { get; set; } = "";
        public DateTime LastLogin { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsOnline { get; set; }
    }
}
