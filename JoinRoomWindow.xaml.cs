using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class JoinRoomWindow : Window
    {
        private List<RoomInfo> rooms = new();

        public JoinRoomWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4)));
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 64) this.DragMove();
            };

            LoadRooms();
        }

        /// <summary>
        /// 从CreateRoomWindow创建房间后打开，新建的房间置顶显示
        /// </summary>
        public JoinRoomWindow(RoomInfo createdRoom) : this()
        {
            // 将新建房间插到列表顶部并选中
            rooms.Insert(0, createdRoom);
            // 窗口加载后滚动到顶部并高亮
            this.Loaded += (s, e) =>
            {
                RenderRooms(rooms);
            };
        }

        private void LoadRooms()
        {
            rooms.Add(new RoomInfo
            {
                Name = "新手友好房",
                Map = "de_dust2",
                ServerRegion = "CN",
                CurrentPlayers = 3,
                MaxPlayers = 10,
                Mode = "休闲",
                ConnectIP = "127.0.0.1:27015"
            });
            rooms.Add(new RoomInfo
            {
                Name = "高手竞技",
                Map = "de_mirage",
                ServerRegion = "CN",
                CurrentPlayers = 8,
                MaxPlayers = 10,
                Mode = "竞技",
                ConnectIP = "127.0.0.1:27016"
            });
            rooms.Add(new RoomInfo
            {
                Name = "Inferno 5v5",
                Map = "de_inferno",
                ServerRegion = "HK",
                CurrentPlayers = 6,
                MaxPlayers = 10,
                Mode = "竞技",
                ConnectIP = "127.0.0.1:27017"
            });
            rooms.Add(new RoomInfo
            {
                Name = "娱乐乱斗",
                Map = "de_nuke",
                ServerRegion = "JP",
                CurrentPlayers = 2,
                MaxPlayers = 20,
                Mode = "休闲",
                ConnectIP = "127.0.0.1:27018"
            });
            rooms.Add(new RoomInfo
            {
                Name = "Overpass 训练",
                Map = "de_overpass",
                ServerRegion = "CN",
                CurrentPlayers = 4,
                MaxPlayers = 10,
                Mode = "休闲",
                ConnectIP = "127.0.0.1:27019"
            });
            rooms.Add(new RoomInfo
            {
                Name = "Anubis 新图体验",
                Map = "de_anubis",
                ServerRegion = "HK",
                CurrentPlayers = 5,
                MaxPlayers = 10,
                Mode = "竞技",
                ConnectIP = "127.0.0.1:27020"
            });

            RenderRooms(rooms);
        }

        private void RenderRooms(List<RoomInfo> roomList)
        {
            roomContainer.Children.Clear();
            txtRoomCount.Text = $"{roomList.Count} 个房间";

            for (int i = 0; i < roomList.Count; i++)
            {
                var room = roomList[i];
                var card = CreateRoomCard(room);
                card.Opacity = 0;
                roomContainer.Children.Add(card);

                // 交错进入动画
                var delay = TimeSpan.FromSeconds(0.05 * i);
                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                {
                    BeginTime = delay,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnim = new DoubleAnimation(30, 0, TimeSpan.FromSeconds(0.5))
                {
                    BeginTime = delay,
                    EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut }
                };

                card.BeginAnimation(OpacityProperty, opacityAnim);
                ((Border)card.Child).RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
            }
        }

        private Border CreateRoomCard(RoomInfo room)
        {
            var card = new Border
            {
                Style = (Style)FindResource("RoomCard"),
                Tag = room,
                RenderTransform = new ScaleTransform(1, 1)
            };

            var stack = new StackPanel();

            // 顶部：模式标签 + 人数
            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var modeBadge = new Border
            {
                Background = room.Mode == "竞技"
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F43F5E"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4)
            };
            var modeText = new TextBlock
            {
                Text = room.Mode,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            modeBadge.Child = modeText;
            Grid.SetColumn(modeBadge, 0);

            var regionText = new TextBlock
            {
                Text = $"  {room.ServerRegion}",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(regionText, 1);

            var playerCount = new TextBlock
            {
                Text = $"{room.CurrentPlayers}/{room.MaxPlayers}",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(playerCount, 2);

            topRow.Children.Add(modeBadge);
            topRow.Children.Add(regionText);
            topRow.Children.Add(playerCount);

            // 房间名
            var nameText = new TextBlock
            {
                Text = room.Name,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                Margin = new Thickness(0, 12, 0, 4)
            };

            // 地图
            var mapText = new TextBlock
            {
                Text = room.Map,
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                FontFamily = (FontFamily)FindResource("FontMono")
            };

            // 进度条
            var progressBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F")),
                CornerRadius = new CornerRadius(3),
                Height = 6,
                Margin = new Thickness(0, 16, 0, 0)
            };
            var progressFill = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                CornerRadius = new CornerRadius(3),
                Width = 280 * room.CurrentPlayers / room.MaxPlayers,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            progressBar.Child = progressFill;

            stack.Children.Add(topRow);
            stack.Children.Add(nameText);
            stack.Children.Add(mapText);
            stack.Children.Add(progressBar);

            // 内层Border用于入场位移动画（TranslateTransform）
            // 外层Border的ScaleTransform由Style.Trigger控制hover缩放
            var innerBorder = new Border
            {
                RenderTransform = new TranslateTransform(0, 30)
            };
            innerBorder.Child = stack;
            card.Child = innerBorder;
            card.MouseLeftButtonDown += (s, e) =>
            {
                var roomDetail = new RoomDetailWindow(room, false) { Owner = this };
                roomDetail.ShowDialog();
            };

            return card;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = txtSearch.Text.ToLower();
            var filtered = rooms.Where(r =>
                r.Name.ToLower().Contains(query) ||
                r.Map.ToLower().Contains(query)).ToList();
            RenderRooms(filtered);
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e) => RenderRooms(rooms);
        private void FilterCompetitive_Click(object sender, RoutedEventArgs e) =>
            RenderRooms(rooms.Where(r => r.Mode == "竞技").ToList());
        private void FilterCasual_Click(object sender, RoutedEventArgs e) =>
            RenderRooms(rooms.Where(r => r.Mode == "休闲").ToList());

        private void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            var createRoom = new CreateRoomWindow { Owner = this };
            createRoom.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
