using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class MatchInProgressWindow : Window
    {
        private DispatcherTimer? _timer;
        private int _elapsedSeconds;
        private int _ctScore;
        private int _tScore;
        private int _round;

        public ObservableCollection<MatchPlayerStats> CtPlayers { get; set; } = new();
        public ObservableCollection<MatchPlayerStats> TPlayers { get; set; } = new();

        /// <summary>连接地址，传给赛后窗口用</summary>
        public string ServerAddress { get; set; } = "";

        public MatchInProgressWindow()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 6);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

                txtServerAddr.Text = string.IsNullOrEmpty(ServerAddress) ? "127.0.0.1:27015" : ServerAddress;

                ctPlayerList.ItemsSource = CtPlayers;
                tPlayerList.ItemsSource = TPlayers;

                StartTimer();
            };

            MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 56) DragMove();
            };
        }

        private void StartTimer()
        {
            _elapsedSeconds = 0;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) =>
            {
                _elapsedSeconds++;
                var span = TimeSpan.FromSeconds(_elapsedSeconds);
                txtMatchTimer.Text = span.TotalMinutes >= 1
                    ? $"{(int)span.TotalMinutes}:{span.Seconds:D2}"
                    : $"0:{span.Seconds:D2}";
            };
            _timer.Start();
        }

        public void UpdateScore(int ctScore, int tScore)
        {
            _ctScore = ctScore;
            _tScore = tScore;
            _round = ctScore + tScore + 1;
            txtCtScore.Text = ctScore.ToString();
            txtTSscore.Text = tScore.ToString();
            txtRoundInfo.Text = ctScore + tScore >= 15 ? "赛点" : $"第 {_round} 回合";
        }

        /// <summary>
        /// 模拟比赛结束后打开结算窗口
        /// </summary>
        public MatchResultData EndMatch()
        {
            _timer?.Stop();
            return new MatchResultData
            {
                CtScore = _ctScore,
                TScore = _tScore,
                CtPlayers = CtPlayers.ToList(),
                TPlayers = TPlayers.ToList(),
                TeamWinner = _ctScore > _tScore ? "CT" : "T",
                MapName = txtMatchMap.Text,
                Duration = _elapsedSeconds
            };
        }

        private void ViewMatchResult_Click(object sender, RoutedEventArgs e)
        {
            var result = EndMatch();
            var resultWin = new MatchResultWindow(result) { Owner = this };
            resultWin.ShowDialog();
        }

        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要离开比赛吗？", "离开比赛",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            _timer?.Stop();
            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin?.ShowToast("已离开比赛");
            AnimationHelper.WindowExit(this, () => Close());
        }
    }

    /// <summary>
    /// 比赛玩家统计数据
    /// </summary>
    public class MatchPlayerStats
    {
        public int Slot { get; set; }
        public string Name { get; set; } = "";
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Score { get; set; }
        public int Headshots { get; set; }
        public int Damage { get; set; }
        public int MVPs { get; set; }

        public int KDRatio => Deaths > 0 ? Kills * 100 / Deaths : Kills * 100;
        public string KDDisplay => Deaths > 0 ? $"{(double)Kills / Deaths:F1}" : $"{Kills}.0";
        public string HeadshotPercent => Kills > 0 ? $"{Headshots * 100 / Kills}%" : "0%";
    }

    /// <summary>
    /// 比赛结果数据，传给 MatchResultWindow
    /// </summary>
    public class MatchResultData
    {
        public int CtScore { get; set; }
        public int TScore { get; set; }
        public string TeamWinner { get; set; } = "";
        public string MapName { get; set; } = "";
        public int Duration { get; set; }
        public int MMRChange { get; set; }
        public System.Collections.Generic.List<MatchPlayerStats> CtPlayers { get; set; } = new();
        public System.Collections.Generic.List<MatchPlayerStats> TPlayers { get; set; } = new();

        public string DurationDisplay => TimeSpan.FromSeconds(Duration).TotalMinutes >= 1
            ? $"{(int)TimeSpan.FromSeconds(Duration).TotalMinutes}m {TimeSpan.FromSeconds(Duration).Seconds}s"
            : $"{Duration}s";

        public MatchPlayerStats? GetMVP()
        {
            var all = CtPlayers.Concat(TPlayers).ToList();
            return all.OrderByDescending(p => p.Score).FirstOrDefault();
        }

        public string WinnerText => TeamWinner == "CT" ? "CT (反恐精英)" : "T (恐怖分子)";
    }
}
