using BeiShui.ApiGateway.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[Route("api/matches")]
public class MatchesController : BaseController
{
    private readonly AppDbContext _db;

    public MatchesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMatches([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var matches = await _db.Matches
            .Where(m => m.Status == 1)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(m => new
            {
                m.Id, m.MapName, m.Mode, m.DurationSeconds,
                m.WinnerTeam, m.CreatedAt,
                PlayerCount = _db.MatchPlayers.Count(mp => mp.MatchId == m.Id)
            })
            .ToListAsync();

        return Ok(matches);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMatch(long id)
    {
        var match = await _db.Matches.FindAsync(id);
        if (match == null) return NotFound();

        var players = await _db.MatchPlayers
            .Include(mp => mp.User)
            .Where(mp => mp.MatchId == id)
            .ToListAsync();

        return Ok(new
        {
            match.Id, match.MapName, match.Mode, match.Status,
            match.WinnerTeam, match.DurationSeconds, match.CreatedAt, match.EndedAt,
            Players = players.Select(p => new
            {
                p.User?.Username, p.User?.Nickname, p.Team,
                p.Kills, p.Deaths, p.Assists, p.Headshots,
                p.Damage, p.MVPs, p.Score, p.MMRChange, p.IsWinner
            })
        });
    }

    /// <summary>
    /// CS2 服务器插件上报比赛结果
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> ReportMatch([FromBody] ReportMatchRequest req)
    {
        var match = new Models.Match
        {
            GameId = 1,
            MapName = req.MapName,
            Mode = req.Mode,
            Status = 1,
            WinnerTeam = req.WinnerTeam,
            DurationSeconds = req.DurationSeconds,
            CreatedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow
        };

        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        foreach (var p in req.Players)
        {
            _db.MatchPlayers.Add(new Models.MatchPlayer
            {
                MatchId = match.Id,
                UserId = p.UserId,
                Team = p.Team,
                Kills = p.Kills,
                Deaths = p.Deaths,
                Assists = p.Assists,
                Headshots = p.Headshots,
                Damage = p.Damage,
                MVPs = p.MVPs,
                Score = p.Score,
                IsWinner = p.Team == req.WinnerTeam,
                JoinedAt = DateTime.UtcNow
            });

            // 更新用户统计
            var user = await _db.Users.FindAsync(p.UserId);
            if (user != null)
            {
                user.KillCount += p.Kills;
                user.HeadshotCount += p.Headshots;
                user.TotalGames++;
                if (p.Team == req.WinnerTeam)
                    user.WinCount++;
                else
                    user.LoseCount++;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { MatchId = match.Id, Message = "比赛记录已保存" });
    }

    public class ReportMatchRequest
    {
        public string MapName { get; set; } = "";
        public int Mode { get; set; }
        public int WinnerTeam { get; set; }
        public int DurationSeconds { get; set; }
        public List<ReportPlayer> Players { get; set; } = new();
    }

    public class ReportPlayer
    {
        public long UserId { get; set; }
        public int Team { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Headshots { get; set; }
        public int Damage { get; set; }
        public int MVPs { get; set; }
        public int Score { get; set; }
    }

    [HttpGet("stats/{userId}")]
    public async Task<IActionResult> GetUserStats(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var rank = await _db.Ranks.FindAsync(user.RankId);
        var recentMatches = await _db.MatchPlayers
            .Include(mp => mp.Match)
            .Where(mp => mp.UserId == userId && mp.Match!.Status == 1)
            .OrderByDescending(mp => mp.Match!.CreatedAt)
            .Take(20)
            .ToListAsync();

        return Ok(new
        {
            user.MMR, RankName = rank?.Name ?? "未定级",
            user.WinCount, user.LoseCount, user.KillCount, user.HeadshotCount, user.TotalGames,
            WinRate = user.TotalGames > 0 ? (double)user.WinCount / user.TotalGames * 100 : 0,
            RecentMatches = recentMatches.Select(mp => new
            {
                mp.Match?.MapName, mp.Kills, mp.Deaths, mp.Assists, mp.IsWinner,
                mp.Match?.DurationSeconds, mp.Match?.CreatedAt
            })
        });
    }
}
