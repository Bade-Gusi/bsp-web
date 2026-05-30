using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeiShui.ServerPlugin;

/// <summary>
/// 背水对战平台 CS2 服务器插件
/// 比赛结束时上报对战数据到 BeiShui API
///
/// 安装:
/// 1. 安装 CounterStrikeSharp (https://docs.cssharp.dev/)
/// 2. 将编译后的 BeiShui.ServerPlugin.dll 放入 csgo/addons/counterstrikesharp/plugins/BeiShui.ServerPlugin/
/// 3. 修改 config/addons/counterstrikesharp/configs/plugins/BeiShui.ServerPlugin.json 中的 ApiUrl
/// </summary>
public class BeiShuiGamePlugin : BasePlugin
{
    public override string ModuleName => "BeiShui Game Plugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "BeiShui Studio";

    private const string DefaultApiUrl = "http://localhost:5000";
    private string _apiUrl = DefaultApiUrl;
    private readonly HttpClient _http = new();
    private readonly Dictionary<ulong, PlayerStats> _playerStats = new();

    public override void Load(bool hotReload)
    {
        // 从配置读取 API 地址
        _apiUrl = GetConfigValue("ApiUrl") ?? DefaultApiUrl;

        // 玩家连接时初始化统计数据
        RegisterListener<Listeners.OnClientConnected>(slot =>
        {
            var player = Utilities.GetPlayerFromSlot(slot);
            if (player?.SteamID != null)
            {
                _playerStats[player.SteamID] = new PlayerStats();
            }
        });

        // 玩家断开时清理
        RegisterListener<Listeners.OnClientDisconnect>(slot =>
        {
            var player = Utilities.GetPlayerFromSlot(slot);
            if (player?.SteamID != null)
            {
                _playerStats.Remove(player.SteamID);
            }
        });

        // 击杀事件
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            var attacker = @event.Attacker;
            var victim = @event.Victim;
            var weapon = @event.Weapon;

            if (attacker?.SteamID != null && _playerStats.ContainsKey(attacker.SteamID))
            {
                _playerStats[attacker.SteamID].Kills++;
                if (@event.Headshot)
                    _playerStats[attacker.SteamID].Headshots++;
            }

            if (victim?.SteamID != null && _playerStats.ContainsKey(victim.SteamID))
            {
                _playerStats[victim.SteamID].Deaths++;
            }

            return HookResult.Continue;
        });

        // 比赛结束上报数据
        RegisterEventHandler<EventCsWinPanelMatch>((@event, info) =>
        {
            // 延迟上报，等待比赛完全结束
            Server.NextFrame(async () =>
            {
                await ReportMatchResult();
            });
            return HookResult.Continue;
        });

        // 管理命令
        AddCommand("css_beishui_api", "设置API地址: !beishui_api <url>", (player, info) =>
        {
            if (player == null || !player.IsAdmin) return;
            if (info.ArgCount < 2) return;

            _apiUrl = info.GetArg(1);
            SaveConfigValue("ApiUrl", _apiUrl);
            player.PrintToChat($"API 地址已设置为: {_apiUrl}");
        });

        AddCommand("css_beishui_status", "查看插件状态", (player, info) =>
        {
            if (player == null) return;
            player.PrintToChat($"[BeiShui] API: {_apiUrl}");
            player.PrintToChat($"[BeiShui] 追踪中玩家: {_playerStats.Count}");
        });
    }

    private async Task ReportMatchResult()
    {
        try
        {
            var players = Utilities.GetPlayers();
            var matchPlayers = new List<object>();
            int ctScore = 0, tScore = 0;
            int winnerTeam = 0;

            foreach (var p in players)
            {
                if (p?.SteamID == null || p.IsBot) continue;

                var stats = _playerStats.GetValueOrDefault(p.SteamID);
                if (stats == null) continue;

                // 获取用户 ID（通过 SteamID 映射）
                var userId = await GetUserIdBySteamId(p.SteamID.ToString());
                if (userId <= 0) continue;

                var team = p.TeamNum;
                matchPlayers.Add(new
                {
                    UserId = userId,
                    Team = team,
                    Kills = stats.Kills,
                    Deaths = stats.Deaths,
                    Assists = 0,
                    Headshots = stats.Headshots,
                    Damage = 0,
                    MVPs = 0,
                    Score = stats.Kills * 3 + stats.Headshots * 2
                });

                if (team == 2) ctScore++;
                else if (team == 3) tScore++;

                // 重置统计数据
                stats.Reset();
            }

            winnerTeam = ctScore > tScore ? 2 : 3;

            var payload = new
            {
                MapName = Server.MapName,
                Mode = 0,
                WinnerTeam = winnerTeam,
                DurationSeconds = (int)(Server.CurrentTime),
                Players = matchPlayers
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{_apiUrl}/api/matches/report", content);

            if (response.IsSuccessStatusCode)
            {
                Server.PrintToChatAll(" [背水对战] 比赛结果已保存");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BeiShui] 上报比赛结果失败: {ex.Message}");
        }
    }

    private async Task<long> GetUserIdBySteamId(string steamId)
    {
        try
        {
            var response = await _http.GetAsync($"{_apiUrl}/api/users/steam/{steamId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("id", out var id))
                    return id.GetInt64();
            }
        }
        catch { }
        return 0;
    }

    private string? GetConfigValue(string key)
    {
        try
        {
            var configPath = Path.Combine(ModuleDirectory, "config.json");
            if (!File.Exists(configPath)) return null;
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var value))
                return value.GetString();
        }
        catch { }
        return null;
    }

    private void SaveConfigValue(string key, string value)
    {
        try
        {
            var configPath = Path.Combine(ModuleDirectory, "config.json");
            Dictionary<string, string> config;
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            else
            {
                config = new();
            }
            config[key] = value;
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private class PlayerStats
    {
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Headshots { get; set; }

        public void Reset()
        {
            Kills = 0;
            Deaths = 0;
            Headshots = 0;
        }
    }
}
