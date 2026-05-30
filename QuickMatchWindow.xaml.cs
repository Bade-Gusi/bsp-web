using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class QuickMatchWindow : Window
    {
        private DispatcherTimer? _timer;
        private int _elapsedSeconds;
        private bool _isSearching;
        private bool _matchFound;
        private string? _currentMatchId;
        private string? _connectAddress;

        // SignalR 事件处理
        private Action<object>? _onMatchFoundHandler;
        private Action<object>? _onQueueStatusHandler;
        private Action<object>? _onServerReadyHandler;

        public QuickMatchWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 14);
                AnimationHelper.WindowEntrance(this);
                StartPulseAnimation();
                StartSearch();
            };
        }

        private void StartPulseAnimation()
        {
            pulseRing.RenderTransformOrigin = new Point(0.5, 0.5);
            pulseRing.RenderTransform = new ScaleTransform(1, 1);

            var pulseAnim = new DoubleAnimation(0.85, 1.18, TimeSpan.FromSeconds(1.4))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 3, EasingMode = EasingMode.EaseInOut }
            };
            pulseRing.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnim);
            pulseRing.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnim);
        }

        private async void StartSearch()
        {
            if (_isSearching) return;
            _isSearching = true;
            _matchFound = false;
            _elapsedSeconds = 0;

            // 注册 SignalR 事件
            if (App.SignalR != null)
            {
                _onMatchFoundHandler = (data) => Dispatcher.Invoke(() => OnMatchFound(data));
                _onQueueStatusHandler = (data) => Dispatcher.Invoke(() => OnQueueStatus(data));
                _onServerReadyHandler = (data) => Dispatcher.Invoke(() => OnServerReady(data));

                App.SignalR.OnMatchFound(_onMatchFoundHandler);
                App.SignalR.OnQueueStatus(_onQueueStatusHandler);
                App.SignalR.OnServerReady(_onServerReadyHandler);

                // 加入队列
                await App.SignalR.JoinQueue(1, 0);
            }

            txtStatus.Text = "正在搜索对手...";
            tipBar.Visibility = Visibility.Visible;
            txtTip.Text = "💡 正在为你寻找实力相近的对手";

            // 计时器
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                _elapsedSeconds++;
                txtTimer.Text = $"{_elapsedSeconds / 60:D2}:{_elapsedSeconds % 60:D2}";
            };
            _timer.Start();
        }

        private void OnQueueStatus(object data)
        {
            dynamic? status = data;
            int queueCount = 0;
            try { queueCount = status?.QueueCount ?? 0; } catch { }
            txtQueueCount.Text = $"{queueCount} 人在队列中";
        }

        private void OnMatchFound(object data)
        {
            if (_matchFound) return;
            _matchFound = true;
            _isSearching = false;
            _timer?.Stop();

            // 停止脉冲动画
            pulseRing.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            pulseRing.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            // 成功动画
            txtStatus.Text = "匹配成功！";
            txtStatus.Foreground = (Brush)FindResource("PrimaryBrush");
            tipBar.Visibility = Visibility.Collapsed;

            var celebrate = new DoubleAnimation(1, 1.35, TimeSpan.FromSeconds(0.4));
            celebrate.EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut };
            pulseRing.BeginAnimation(Border.OpacityProperty,
                new DoubleAnimation(1, 0.7, TimeSpan.FromSeconds(1))
                { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });

            // 解析匹配数据
            string opponentName = "";
            int opponentMMR = 0;
            string mapName = "de_dust2";
            dynamic? match = data;
            if (match != null)
            {
                try { _currentMatchId = match.MatchId; } catch { }
                try { opponentName = match.OpponentName ?? ""; } catch { }
                try { opponentMMR = match.OpponentMMR ?? 0; } catch { }
                try { mapName = match.Map ?? "de_dust2"; } catch { }
            }

            // 弹出接受匹配窗口（传对手数据）
            var acceptWin = new AcceptMatchWindow
            {
                Owner = this,
                OpponentName = opponentName,
                OpponentMMR = opponentMMR,
                Map = mapName,
                MyName = App.CurrentUser?.Username ?? "",
                MyMMR = App.CurrentUser?.Elo ?? 0
            };
            bool? accepted = acceptWin.ShowDialog();

            if (accepted == true)
            {
                // 接受匹配
                _ = AcceptMatchAsync();
            }
            else
            {
                // 拒绝匹配，重新搜索
                _ = RejectAndRequeueAsync();
            }
        }

        private async System.Threading.Tasks.Task AcceptMatchAsync()
        {
            if (!string.IsNullOrEmpty(_currentMatchId) && App.SignalR != null)
            {
                await App.SignalR.AcceptMatch(_currentMatchId);
                txtStatus.Text = "正在等待服务器就绪...";
            }
        }

        private async System.Threading.Tasks.Task RejectAndRequeueAsync()
        {
            _matchFound = false;
            if (!string.IsNullOrEmpty(_currentMatchId) && App.SignalR != null)
            {
                await App.SignalR.RejectMatch(_currentMatchId, 1, 0);
            }
            txtStatus.Text = "已取消，重新匹配中...";
            _isSearching = true;
            _timer?.Start();
        }

        private void OnServerReady(object data)
        {
            dynamic? serverInfo = data;
            if (serverInfo != null)
            {
                try { _connectAddress = serverInfo.ConnectAddress; } catch { }
            }

            LaunchGame();
        }

        private void LaunchGame()
        {
            if (App.AntiCheatBlocked)
            {
                var mainWin = Application.Current.MainWindow as MainWindow;
                mainWin?.ShowToast("⛔ 反作弊已封锁，无法启动游戏");
                DialogHelper.ShowAntiCheatBlocked(this);
                return;
            }

            string serverAddress = _connectAddress ?? AppSettings.Load().LastSelectedServerIP;
            if (string.IsNullOrWhiteSpace(serverAddress))
                serverAddress = "127.0.0.1:27015";

            try { Clipboard.SetText(serverAddress); } catch { }

            GameLauncher.ConnectAndHeartbeat(serverAddress);

            // 打开比赛界面
            var matchWin = new MatchInProgressWindow
            {
                ServerAddress = serverAddress,
                Owner = Application.Current.MainWindow
            };
            // 填充示例数据（实际应由后端推送）
            for (int i = 1; i <= 5; i++)
            {
                matchWin.CtPlayers.Add(new MatchPlayerStats
                {
                    Slot = i, Name = $"队友{i}", Kills = 12 + i * 2,
                    Deaths = 8 + i, Assists = 3 + i, Score = 80 + i * 10,
                    Headshots = 5 + i, Damage = 2000 + i * 300, MVPs = i % 3
                });
                matchWin.TPlayers.Add(new MatchPlayerStats
                {
                    Slot = i, Name = $"对手{i}", Kills = 10 + i,
                    Deaths = 12 + i, Assists = 2 + i, Score = 70 + i,
                    Headshots = 3 + i, Damage = 1800 + i * 200, MVPs = i % 2
                });
            }
            matchWin.Show();

            // 模拟比赛结束（测试用，实际应由服务端推送）
            // matchWin.UpdateScore(13, 9);

            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CleanupAndClose();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CleanupAndClose();
        }

        private void CleanupAndClose()
        {
            _isSearching = false;
            _timer?.Stop();

            // 离开队列
            if (App.SignalR != null && !_matchFound)
            {
                _ = App.SignalR.LeaveQueue(1, 0);
            }

            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin?.ShowToast("已取消匹配");

            AnimationHelper.WindowExit(this, () => this.Close());
        }

        protected override void OnClosed(EventArgs e)
        {
            CleanupAndClose();
            base.OnClosed(e);
        }
    }
}
