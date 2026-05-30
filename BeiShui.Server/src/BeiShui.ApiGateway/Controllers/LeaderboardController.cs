using BeiShui.ApiGateway.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[Route("api/leaderboard")]
public class LeaderboardController : BaseController
{
    private readonly AppDbContext _db;

    public LeaderboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int page = 1, [FromQuery] int size = 100)
    {
        var leaders = await _db.Users
            .OrderByDescending(u => u.MMR)
            .Skip((page - 1) * size).Take(size)
            .Select(u => new
            {
                u.Id, u.Username, u.Nickname, u.MMR, u.WinCount, u.LoseCount, u.TotalGames
            })
            .ToListAsync();

        return Ok(leaders);
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
            user.MMR,
            RankName = rank?.Name ?? "未定级",
            user.WinCount,
            user.LoseCount,
            user.KillCount,
            user.HeadshotCount,
            user.TotalGames,
            WinRate = user.TotalGames > 0 ? (double)user.WinCount / user.TotalGames * 100 : 0,
            RecentMatches = recentMatches.Select(mp => new
            {
                mp.Match?.MapName,
                mp.Kills,
                mp.Deaths,
                mp.Assists,
                mp.IsWinner,
                mp.Match?.DurationSeconds,
                mp.Match?.CreatedAt
            })
        });
    }
}
