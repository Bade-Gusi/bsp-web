using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BeiShuiCS2
{
    public partial class MatchSetupWindow : Window
    {
        private string selectedMap = "de_dust2";
        private string selectedMode = "竞技";
        private string selectedRegion = "CN";
        private int maxPlayers = 10;
        private Border? selectedMapCard;

        public MatchSetupWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };

            LoadFriends();
            SelectMap("de_dust2");
            AnimateEntrance();
        }

        private void AnimateEntrance()
        {
            var items = new FrameworkElement[] { headerPanel, mapSection, modeSection, regionSection, friendSection, bottomPanel };
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                items[i].Opacity = 0;
                items[i].RenderTransform = new TranslateTransform(0, 30);

                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(0.08 * i),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnim = new DoubleAnimation(30, 0, TimeSpan.FromSeconds(0.5))
                {
                    BeginTime = TimeSpan.FromSeconds(0.08 * i),
                    EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut }
                };

                items[i].BeginAnimation(OpacityProperty, opacityAnim);
                items[i].RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
            }
        }

        private void LoadFriends()
        {
            var friends = new[] { "Player1", "Player2", "Player3" };
            foreach (var friend in friends)
            {
                var chip = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 8, 8),
                    Cursor = Cursors.Hand,
                    Tag = false // 是否已邀请
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };
                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                stack.Children.Add(dot);
                stack.Children.Add(new TextBlock
                {
                    Text = friend,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                });
                chip.Child = stack;

                chip.MouseLeftButtonDown += (s, e) =>
                {
                    if (s is Border b)
                    {
                        bool invited = (bool)b.Tag;
                        b.Tag = !invited;
                        b.Background = invited
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F"))
                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D4A35"));
                        b.Opacity = 1;
                        // 缩放动画
                        var pulse = new DoubleAnimation(0.95, 1, TimeSpan.FromSeconds(0.2))
                        {
                            EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
                        };
                        b.RenderTransform = new ScaleTransform(1, 1);
                        b.RenderTransformOrigin = new Point(0.5, 0.5);
                        b.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
                        b.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
                    }
                };

                friendChips.Children.Add(chip);
            }
        }

        private void SelectMap(string map)
        {
            selectedMap = map;
            var wrapPanel = FindMapCardsWrapPanel();
            if (wrapPanel == null) return;

            foreach (var child in wrapPanel.Children)
            {
                if (child is Border card)
                {
                    card.Style = (Style)FindResource("MapCard");
                    if (card.RenderTransform == null || card.RenderTransform is ScaleTransform)
                    {
                        card.RenderTransform = new ScaleTransform(1, 1);
                        card.RenderTransformOrigin = new Point(0.5, 0.5);
                    }
                }
            }
        }

        private WrapPanel? FindMapCardsWrapPanel()
        {
            if (this.Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Grid innerGrid)
                    {
                        foreach (var c in innerGrid.Children)
                        {
                            if (c is StackPanel sp)
                            {
                                foreach (var item in sp.Children)
                                {
                                    if (item is WrapPanel wp && wp.Children.Count > 0)
                                        return wp;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void Map_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                string? mapName = border.Tag?.ToString();
                if (string.IsNullOrEmpty(mapName)) return;

                selectedMap = mapName;
                SelectMap(mapName);
                selectedMapCard = border;

                // 点击动画：缩小→恢复带回弹
                var scaleAnim = new DoubleAnimation(1, 0.95, TimeSpan.FromSeconds(0.1));
                var scaleBack = new DoubleAnimation(0.95, 1, TimeSpan.FromSeconds(0.25));
                scaleBack.EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };

                if (border.RenderTransform is not ScaleTransform)
                {
                    border.RenderTransform = new ScaleTransform(1, 1);
                    border.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                var st = (ScaleTransform)border.RenderTransform;
                st.ScaleX = 1;
                st.ScaleY = 1;

                scaleAnim.Completed += (s, _) =>
                {
                    st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleBack);
                    st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleBack);
                };

                st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
        }

        private void Mode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                selectedMode = btn.Tag?.ToString() ?? "竞技";

                btnModeCompetitive.Style = (Style)FindResource("BtnGhost");
                btnModeCasual.Style = (Style)FindResource("BtnGhost");
                btnModeWingman.Style = (Style)FindResource("BtnGhost");
                btn.Style = (Style)FindResource("BtnPrimary");

                // 根据模式更新最大玩家数
                maxPlayers = selectedMode == "双人" ? 4 : (selectedMode == "休闲" ? 20 : 10);
                txtMaxPlayers.Text = maxPlayers.ToString();
            }
        }

        private void Region_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                selectedRegion = btn.Tag?.ToString() ?? "CN";

                btnRegionCN.Style = (Style)FindResource("BtnGhost");
                btnRegionHK.Style = (Style)FindResource("BtnGhost");
                btnRegionJP.Style = (Style)FindResource("BtnGhost");
                btn.Style = (Style)FindResource("BtnPrimary");
            }
        }

        private void BtnCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string roomName = txtRoomName.Text.Trim();
            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomName = $"{selectedMap} {selectedMode}房";
            }

            // 验证房间名
            var validation = NameValidator.ValidateChatText(roomName);
            if (!validation.IsValid)
            {
                ShakeControl(txtRoomName);
                ShowToast(validation.ErrorMessage);
                return;
            }

            string password = txtRoomPassword.Password;

            // 按钮动画
            var scaleAnim = new DoubleAnimation(1, 0.95, TimeSpan.FromSeconds(0.1));
            var scaleBack = new DoubleAnimation(0.95, 1, TimeSpan.FromSeconds(0.2));
            scaleBack.EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };

            btnCreateRoom.RenderTransform = new ScaleTransform(1, 1);
            btnCreateRoom.RenderTransformOrigin = new Point(0.5, 0.5);

            scaleAnim.Completed += (s, _) =>
            {
                btnCreateRoom.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleBack);
                btnCreateRoom.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleBack);

                try
                {
                    // 创建房间成功 — 打开房间大厅并选中新建的房间
                    var roomInfo = new RoomInfo
                    {
                        RoomId = Guid.NewGuid().ToString("N"),
                        Name = roomName,
                        Map = selectedMap,
                        ServerRegion = selectedRegion,
                        CurrentPlayers = 1,
                        MaxPlayers = maxPlayers,
                        Mode = selectedMode,
                        ConnectIP = "127.0.0.1:" + new Random().Next(27015, 27200),
                        HasPassword = !string.IsNullOrEmpty(password)
                    };

                    var joinRoom = new JoinRoomWindow(roomInfo) { Owner = this.Owner };
                    joinRoom.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建房间失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnCreateRoom.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            btnCreateRoom.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        }

        private void ShakeControl(Control control)
        {
            var shake = new DoubleAnimation(0, 6, TimeSpan.FromSeconds(0.05))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };
            var translate = new TranslateTransform();
            control.RenderTransform = translate;
            translate.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        private void ShowToast(string message)
        {
            txtError.Text = "⚠ " + message;
            txtError.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
            txtError.BeginAnimation(OpacityProperty, fadeIn);

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, _) =>
            {
                timer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                fadeOut.Completed += (s2, _) => txtError.Visibility = Visibility.Collapsed;
                txtError.BeginAnimation(OpacityProperty, fadeOut);
            };
            timer.Start();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseWithAnimation();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CloseWithAnimation();
        }

        private void CloseWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
