using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class MatchingWindow : Window
    {
        private DispatcherTimer? _timer;
        private int _elapsedSeconds;
        private bool _isSearching;
        private bool _matchFound;
        private string? _currentMatchId;
        private string? _connectAddress;

        public string SelectedMap { get; }
        public string SelectedRegion { get; }

        public MatchingWindow() : this("de_dust2", "CN") { }

        public MatchingWindow(string map, string region)
        {
            InitializeComponent();

            SelectedMap = map;
            SelectedRegion = region;
            txtMapName.Text = string.IsNullOrEmpty(map) ? "de_dust2" : map;

            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 16);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4)));
                StartVSPulse();
                StartSearch();
            };
        }

        private void StartVSPulse()
        {
            var pulseOpacity = new DoubleAnimation(0.5, 1, TimeSpan.FromSeconds(1.2))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            txtVS.BeginAnimation(TextBlock.OpacityProperty, pulseOpacity);
        }

        private async void StartSearch()
        {
            if (_isSearching) return;
            _isSearching = true;
            _elapsedSeconds = 0;

            UpdateUI(MatchState.Searching);

            // 注册 SignalR 事件
            if (App.SignalR != null)
            {
                App.SignalR.OnMatchFound(OnMatchFound);
                App.SignalR.OnQueueStatus(OnQueueStatus);
                App.SignalR.OnServerReady(OnServerReady);

                await App.SignalR.JoinQueue(1, 0);
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                _elapsedSeconds++;
                int mins = _elapsedSeconds / 60;
                int secs = _elapsedSeconds % 60;
                txtTimer.Text = $"{mins:D2}:{secs:D2}";
            };
            _timer.Start();
        }

        private void OnQueueStatus(object data)
        {
        }

        private void OnMatchFound(object data)
        {
            if (_matchFound) return;
            _matchFound = true;

            Dispatcher.Invoke(() =>
            {
                _isSearching = false;
                _timer?.Stop();

                dynamic? match = data;
                string? team = null;
                try { _currentMatchId = match.MatchId; team = match.Team; } catch { }

                UpdateUI(MatchState.Ready);

                // 显示队伍信息
                if (team == "blue")
                {
                    txtStatus.Text = "匹配成功！";
                    statusBadge.Background = (Brush)FindResource("PrimaryBrush");
                }
                else
                {
                    txtStatus.Text = "匹配成功！";
                    statusBadge.Background = (Brush)FindResource("AccentBrush");
                }

                // 解析对手信息
                string opponentName = "";
                int opponentMMR = 0;
                try { opponentName = match?.Opponents?[0]?.Name ?? ""; } catch { }
                try { opponentMMR = match?.Opponents?[0]?.MMR ?? 0; } catch { }

                // 弹出接受窗口
                var acceptWin = new AcceptMatchWindow
                {
                    Owner = this,
                    OpponentName = opponentName,
                    OpponentMMR = opponentMMR,
                    Map = "de_dust2",
                    MyName = App.CurrentUser?.Username ?? "",
                    MyMMR = App.CurrentUser?.Elo ?? 0
                };
                bool? accepted = acceptWin.ShowDialog();

                if (accepted == true)
                {
                    _ = AcceptMatchAsync();
                }
                else
                {
                    _ = RejectAndRequeueAsync();
                }
            });
        }

        private async System.Threading.Tasks.Task AcceptMatchAsync()
        {
            txtStatus.Text = "连接中...";
            statusBadge.Background = (Brush)FindResource("PrimaryBrush");

            if (!string.IsNullOrEmpty(_currentMatchId) && App.SignalR != null)
            {
                await App.SignalR.AcceptMatch(_currentMatchId);
            }
        }

        private async System.Threading.Tasks.Task RejectAndRequeueAsync()
        {
            _matchFound = false;
            if (!string.IsNullOrEmpty(_currentMatchId) && App.SignalR != null)
            {
                await App.SignalR.RejectMatch(_currentMatchId, 1, 0);
            }
            _isSearching = true;
            _timer?.Start();
            UpdateUI(MatchState.Searching);
        }

        private void OnServerReady(object data)
        {
            Dispatcher.Invoke(() =>
            {
                dynamic? serverInfo = data;
                try { _connectAddress = serverInfo.ConnectAddress; } catch { }

                var mainWin = Application.Current.MainWindow as MainWindow;
                mainWin?.ShowToast("🎮 游戏已启动，祝你好运！");

                if (!string.IsNullOrEmpty(_connectAddress))
                {
                    GameLauncher.ConnectAndHeartbeat(_connectAddress);
                }

                AnimationHelper.WindowExit(this, () => this.Close());
            });
        }

        private void UpdateUI(MatchState state)
        {
            switch (state)
            {
                case MatchState.Searching:
                    txtStatus.Text = "搜索中...";
                    statusBadge.Background = (Brush)FindResource("WarningBrush");
                    searchSpinner.Visibility = Visibility.Visible;
                    break;
                case MatchState.Ready:
                    txtStatus.Text = "匹配成功！";
                    statusBadge.Background = (Brush)FindResource("PrimaryBrush");
                    searchSpinner.Visibility = Visibility.Collapsed;
                    break;
                case MatchState.Failed:
                    txtStatus.Text = "匹配失败";
                    statusBadge.Background = (Brush)FindResource("DangerBrush");
                    searchSpinner.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        // XAML 绑定兼容方法
        private void StartMatch_Click(object sender, RoutedEventArgs e) => StartSearch();
        private void RejectMatch_Click(object sender, RoutedEventArgs e) { _ = RejectAndRequeueAsync(); }
        private void AcceptDuel_Click(object sender, RoutedEventArgs e) { _ = AcceptMatchAsync(); }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _isSearching = false;
            _timer?.Stop();

            if (App.SignalR != null && !_matchFound)
            {
                _ = App.SignalR.LeaveQueue(1, 0);
            }

            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin?.ShowToast("已取消匹配");

            AnimationHelper.WindowExit(this, () => this.Close());
        }

        private enum MatchState { Searching, Ready, Failed }
    }
}
