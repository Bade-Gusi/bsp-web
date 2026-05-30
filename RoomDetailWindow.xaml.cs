using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class RoomDetailWindow : Window
    {
        public RoomInfo Room { get; } = null!;
        public bool IsHost { get; }

        public RoomDetailWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));
                AnimationHelper.CreateFloatingParticles(particleCanvas, 10);
                AnimateEntrance();
            };
        }

        private void AnimateEntrance()
        {
            var cards = new FrameworkElement[] { txtRoomName, txtMap, txtServer };
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                int idx = i;
                Dispatcher.BeginInvoke(() =>
                {
                    cards[idx].Opacity = 0;
                    cards[idx].RenderTransform = new TranslateTransform(0, -15);
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                    {
                        BeginTime = TimeSpan.FromSeconds(0.1 * idx),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    var slideDown = new DoubleAnimation(-15, 0, TimeSpan.FromSeconds(0.4))
                    {
                        BeginTime = TimeSpan.FromSeconds(0.1 * idx),
                        EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut }
                    };
                    cards[idx].BeginAnimation(OpacityProperty, fadeIn);
                    cards[idx].RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideDown);
                }, DispatcherPriority.Loaded);
            }
        }

        public RoomDetailWindow(RoomInfo room, bool isHost) : this()
        {
            Room = room;
            IsHost = isHost;
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyIP_Click(object sender, RoutedEventArgs e)
        {
            if (Room?.ConnectIP != null)
                Clipboard.SetText(Room.ConnectIP);
        }

        private void Invite_Click(object sender, RoutedEventArgs e)
        {
            var inviteWindow = new FriendInviteWindow { Owner = this };
            inviteWindow.ShowDialog();
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            var mainWin = Application.Current.MainWindow as MainWindow;

            if (App.AntiCheatBlocked)
            {
                mainWin?.ShowToast("⛔ 反作弊已封锁，无法启动游戏");
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }

            string ip = Room?.ConnectIP ?? AppSettings.Load().LastSelectedServerIP;
            if (string.IsNullOrWhiteSpace(ip))
            {
                var noIpDialog = new ConfirmDialogWindow("无法启动",
                    "未找到服务器地址，请前往服务器管理页面配置。",
                    "确认");
                noIpDialog.Owner = this;
                noIpDialog.ShowDialog();
                return;
            }

            GameLauncher.ConnectAndHeartbeat(ip);

            Application.Current.Dispatcher.Invoke(() =>
            {
                var heartbeatWin = new AntiCheatHeartbeatWindow { Owner = this };
                bool? result = heartbeatWin.ShowDialog();
                if (result != true)
                {
                    mainWin?.ShowToast("⚠️ 心跳验证失败，已断开连接");
                    GameLauncher.StopHeartbeat();
                    return;
                }
                mainWin?.ShowToast("✅ 反作弊心跳验证通过");
            });

            try { Clipboard.SetText(ip); } catch { }
            this.Close();
        }
    }
}
