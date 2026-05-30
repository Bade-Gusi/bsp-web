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
    public partial class CreateRoomWindow : Window
    {
        private string selectedMap = "de_dust2";
        private string selectedMode = "竞技";
        private string selectedRegion = "CN";
        private int maxPlayers = 10;

        public CreateRoomWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 15);
                AnimateContent();
            };

            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };

            SelectMap("de_dust2");
        }

        private void AnimateContent()
        {
            var items = new FrameworkElement[] { };
            // 子元素交错动画通过 Loaded 后的淡入处理
            var panels = new StackPanel?[]
            {
                FindName("roomInfoSection") as StackPanel,
                FindName("modeSection") as StackPanel,
                FindName("mapSection") as StackPanel,
                FindName("regionSection") as StackPanel
            };

            for (int i = 0; i < panels.Length; i++)
            {
                var p = panels[i];
                if (p == null) continue;
                p.Opacity = 0;
                p.RenderTransform = new TranslateTransform(0, 20);

                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                {
                    BeginTime = TimeSpan.FromSeconds(0.08 * i),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnim = new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(0.08 * i),
                    EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut }
                };

                p.BeginAnimation(OpacityProperty, opacityAnim);
                p.RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
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
            if (this.Content is Grid rootGrid)
            {
                foreach (var child in rootGrid.Children)
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

                // 点击动画
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

        private async void BtnCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string roomName = txtRoomName.Text.Trim();
            if (string.IsNullOrWhiteSpace(roomName))
                roomName = $"{selectedMap} {selectedMode}房";

            var validation = NameValidator.ValidateChatText(roomName);
            if (!validation.IsValid)
            {
                ShakeControl(txtRoomName);
                ShowToast(validation.ErrorMessage);
                return;
            }

            string password = txtRoomPassword.Password;

            btnCreateRoom.IsEnabled = false;
            btnCreateRoom.Content = "创建中...";

            var result = await ApiClient.PostAsync<System.Text.Json.JsonElement>("/api/rooms", new
            {
                gameId = 1,
                mapName = selectedMap,
                mode = selectedMode,
                password = string.IsNullOrEmpty(password) ? null : password,
                maxPlayers = maxPlayers
            });

            btnCreateRoom.IsEnabled = true;
            btnCreateRoom.Content = "创建房间";

            if (result.Success && result.Data.ValueKind != System.Text.Json.JsonValueKind.Undefined)
            {
                var roomCode = result.Data.TryGetProperty("roomCode", out var rc) ? rc.GetString() ?? "" : "";
                var roomInfo = new RoomInfo
                {
                    RoomId = roomCode,
                    Name = selectedMap,
                    Map = selectedMap,
                    ServerRegion = selectedRegion,
                    CurrentPlayers = 1,
                    MaxPlayers = maxPlayers,
                    Mode = selectedMode,
                    ConnectIP = "",
                    HasPassword = !string.IsNullOrEmpty(password)
                };

                var joinRoom = new JoinRoomWindow(roomInfo) { Owner = this.Owner };
                joinRoom.Show();
                this.Close();
            }
            else
            {
                ShowToast(result.Error ?? "创建房间失败");
            }
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
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
