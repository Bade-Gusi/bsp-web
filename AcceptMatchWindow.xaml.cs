using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class AcceptMatchWindow : Window
    {
        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds = 15;

        /// <summary>对方用户名</summary>
        public string OpponentName { get; set; } = "";
        /// <summary>对方 MMR</summary>
        public int OpponentMMR { get; set; }
        /// <summary>对方段位</summary>
        public string OpponentRank { get; set; } = "";
        /// <summary>己方用户名</summary>
        public string MyName { get; set; } = "";
        /// <summary>己方 MMR</summary>
        public int MyMMR { get; set; }
        /// <summary>地图</summary>
        public string Map { get; set; } = "de_dust2";

        public AcceptMatchWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

                // 填充数据
                txtMyName.Text = string.IsNullOrEmpty(MyName) ? (App.CurrentUser?.Username ?? "我") : MyName;
                txtMyNameInitial.Text = (MyName.Length > 0 ? MyName[..1] : "我").ToUpper();
                txtMyMMR.Text = MyMMR > 0 ? $"MMR: {MyMMR}" : "";

                txtOpponentName.Text = string.IsNullOrEmpty(OpponentName) ? "对手" : OpponentName;
                txtOpponentInitial.Text = (OpponentName.Length > 0 ? OpponentName[..1] : "?").ToUpper();
                txtOpponentMMR.Text = OpponentMMR > 0 ? $"MMR: {OpponentMMR}" : "";

                txtMap.Text = Map;
                txtMapName.Text = GetMapDisplayName(Map);
                txtAvgMMR.Text = MyMMR > 0 && OpponentMMR > 0 ? $"{(MyMMR + OpponentMMR) / 2}" : "--";
                txtAvgRank.Text = string.IsNullOrEmpty(OpponentRank) ? "黄金" : OpponentRank;

                // 额外统计数据（从传入的属性或从profile获取）
                _ = LoadExtraStats();

                SetupCountdown();
            };
        }

        private void SetupCountdown()
        {
            _remainingSeconds = 15;
            txtCountdown.Text = "15";

            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += (s, e) =>
            {
                _remainingSeconds--;
                txtCountdown.Text = _remainingSeconds.ToString();

                if (_remainingSeconds <= 3)
                {
                    txtCountdown.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#F43F5E"));
                }

                if (_remainingSeconds <= 0)
                {
                    _countdownTimer.Stop();
                    DialogResult = false;
                    Close();
                }
            };
            _countdownTimer.Start();
        }

        private static string GetMapDisplayName(string map)
        {
            return map.ToLower() switch
            {
                "de_dust2" => "炙热沙城Ⅱ (de_dust2)",
                "de_mirage" => "荒漠迷城 (de_mirage)",
                "de_inferno" => "炼狱小镇 (de_inferno)",
                "de_overpass" => "死亡游乐园 (de_overpass)",
                "de_nuke" => "核子危机 (de_nuke)",
                "de_ancient" => "远古遗迹 (de_ancient)",
                "de_vertigo" => "殒命大厦 (de_vertigo)",
                "de_anubis" => "阿努比斯 (de_anubis)",
                _ => map
            };
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer?.Stop();
            DialogResult = true;
            AnimationHelper.WindowExit(this, () => this.Close());
        }

        private async System.Threading.Tasks.Task LoadExtraStats()
        {
            try
            {
                var profile = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/auth/profile");
                if (profile.Success)
                {
                    var total = profile.Data.TryGetProperty("totalGames", out var tg) ? tg.GetInt32() : 0;
                    var wins = profile.Data.TryGetProperty("winCount", out var wc) ? wc.GetInt32() : 0;
                    txtMyWins.Text = $"{wins}";
                    txtMyWinRate.Text = total > 0 ? $"{(int)(wins * 100.0 / total)}%" : "0%";
                    txtMyGames.Text = $"{total}";
                }
            }
            catch { }
            txtOpponentWins.Text = "--"; // 对手数据需从 match 查询
            txtOpponentWinRate.Text = "--";
            txtOpponentGames.Text = "--";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _countdownTimer?.Stop();
            DialogResult = false;
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
