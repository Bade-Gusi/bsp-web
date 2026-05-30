using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class MatchResultWindow : Window
    {
        public MatchResultWindow(long matchId)
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 12);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35)));
                await LoadFromApi(matchId);
            };
        }

        /// <summary>
        /// 兼容旧代码：使用内存数据（降级路径）
        /// </summary>
        public MatchResultWindow(MatchResultData data)
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 12);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35)));
                FillFromData(data);
            };
        }

        private async System.Threading.Tasks.Task LoadFromApi(long matchId)
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement>($"/api/matches/{matchId}");
                if (result.Success && result.Data.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                {
                    var data = result.Data;
                    var ctScore = data.TryGetProperty("winnerTeam", out var wt) && wt.GetInt32() == 1 ? 13 : 0;
                    var tScore = data.TryGetProperty("winnerTeam", out var wt2) && wt2.GetInt32() == 2 ? 13 : 0;

                    txtFinalCtScore.Text = ctScore.ToString();
                    txtFinalTScore.Text = tScore.ToString();
                    txtResultTitle.Text = ctScore > tScore ? "CT 胜利！" : "T 胜利！";
                    txtResultSub.Text = $"比分 {ctScore} : {tScore}";

                    txtDuration.Text = data.TryGetProperty("durationSeconds", out var dur) ? $"{dur.GetInt32()}s" : "--";
                    txtResultMap.Text = data.TryGetProperty("mapName", out var mn) ? mn.GetString() ?? "" : "";

                    // 解析玩家列表
                    var players = new List<ApiPlayerStats>();
                    if (data.TryGetProperty("players", out var pls))
                    {
                        foreach (var p in pls.EnumerateArray())
                        {
                            players.Add(new ApiPlayerStats
                            {
                                Name = (p.TryGetProperty("nickname", out var nn) ? nn.GetString() : null)
                                     ?? (p.TryGetProperty("username", out var un) ? un.GetString() ?? "" : ""),
                                Kills = p.TryGetProperty("kills", out var ki) ? ki.GetInt32() : 0,
                                Deaths = p.TryGetProperty("deaths", out var de) ? de.GetInt32() : 0,
                                Assists = p.TryGetProperty("assists", out var as_) ? as_.GetInt32() : 0,
                                Headshots = p.TryGetProperty("headshots", out var hs) ? hs.GetInt32() : 0,
                                Damage = p.TryGetProperty("damage", out var dg) ? dg.GetInt32() : 0,
                                MVPs = p.TryGetProperty("mvps", out var mv) ? mv.GetInt32() : 0,
                                Score = p.TryGetProperty("score", out var sc) ? sc.GetInt32() : 0,
                                IsWinner = p.TryGetProperty("isWinner", out var iw) && iw.GetBoolean()
                            });
                        }
                    }

                    // MVP
                    var mvp = players.OrderByDescending(p => p.Score).FirstOrDefault();
                    if (mvp != null)
                    {
                        txtMvpName.Text = mvp.Name;
                        txtMvpKDA.Text = $"{mvp.Kills}/{mvp.Deaths}/{mvp.Assists} · {mvp.KDDisplay} K/D";
                        txtMvpScore.Text = mvp.Score.ToString();
                        txtMvpInitial.Text = mvp.Name.Length > 0 ? mvp.Name[..1].ToUpper() : "?";
                    }

                    txtMMRChange.Text = "+25 MMR";
                    ctResultList.ItemsSource = players.OrderByDescending(p => p.Score).ToList();
                }
            }
            catch { }
        }

        private void FillFromData(MatchResultData data)
        {
            txtFinalCtScore.Text = data.CtScore.ToString();
            txtFinalTScore.Text = data.TScore.ToString();

            bool isCtWin = data.TeamWinner == "CT";
            txtResultTitle.Text = isCtWin ? "CT 胜利！" : "T 胜利！";
            txtResultTitle.Foreground = isCtWin
                ? (System.Windows.Media.Brush)FindResource("PrimaryBrush")
                : (System.Windows.Media.Brush)FindResource("WarningBrush");
            txtResultSub.Text = $"{data.WinnerText}\n比分 {data.CtScore} : {data.TScore}";

            var mvp = data.GetMVP();
            if (mvp != null)
            {
                txtMvpName.Text = mvp.Name;
                txtMvpKDA.Text = $"{mvp.Kills}/{mvp.Deaths}/{mvp.Assists} · {mvp.KDDisplay} K/D";
                txtMvpScore.Text = mvp.Score.ToString();
                txtMvpInitial.Text = mvp.Name.Length > 0 ? mvp.Name[..1].ToUpper() : "?";
            }

            txtMMRChange.Text = data.MMRChange >= 0 ? $"+{data.MMRChange} MMR" : $"{data.MMRChange} MMR";
            txtMMRChange.Foreground = data.MMRChange >= 0
                ? (System.Windows.Media.Brush)FindResource("PrimaryBrush")
                : (System.Windows.Media.Brush)FindResource("DangerBrush");

            txtDuration.Text = data.DurationDisplay;
            txtResultMap.Text = data.MapName;

            ctResultList.ItemsSource = data.CtPlayers.Concat(data.TPlayers)
                .OrderByDescending(p => p.Score).ToList();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => Close());
        }

        private class ApiPlayerStats
        {
            public string Name { get; set; } = "";
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Assists { get; set; }
            public int Headshots { get; set; }
            public int Damage { get; set; }
            public int MVPs { get; set; }
            public int Score { get; set; }
            public bool IsWinner { get; set; }
            public string HeadshotPercent => Kills > 0 ? $"{Headshots * 100 / Kills}%" : "0%";
            public string KDDisplay => Deaths > 0 ? $"{(double)Kills / Deaths:F1}" : $"{Kills}.0";
        }
    }
}
