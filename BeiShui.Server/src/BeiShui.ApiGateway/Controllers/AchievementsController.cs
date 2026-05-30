using BeiShui.ApiGateway.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/achievements")]
public class AchievementsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AchievementsController(AppDbContext db) => _db = db;

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetAchievements(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var achievements = new List<object>
        {
            new { Title = "初次登场", Description = "完成第一场比赛", Icon = "🏆", Unlocked = user.TotalGames >= 1, Progress = Math.Min(user.TotalGames, 1), Max = 1 },
            new { Title = "百战勇士", Description = "完成 100 场比赛", Icon = "⚔️", Unlocked = user.TotalGames >= 100, Progress = Math.Min(user.TotalGames, 100), Max = 100 },
            new { Title = "千场老兵", Description = "完成 1000 场比赛", Icon = "🎖️", Unlocked = user.TotalGames >= 1000, Progress = Math.Min(user.TotalGames, 1000), Max = 1000 },
            new { Title = "爆头专家", Description = "累计 500 次爆头", Icon = "🎯", Unlocked = user.HeadshotCount >= 500, Progress = Math.Min(user.HeadshotCount, 500), Max = 500 },
            new { Title = "杀戮机器", Description = "累计 1000 次击杀", Icon = "💀", Unlocked = user.KillCount >= 1000, Progress = Math.Min(user.KillCount, 1000), Max = 1000 },
            new { Title = "百胜将军", Description = "赢得 100 场比赛", Icon = "👑", Unlocked = user.WinCount >= 100, Progress = Math.Min(user.WinCount, 100), Max = 100 },
            new { Title = "连胜大师", Description = "连续赢 5 场（数据同步中）", Icon = "🔥", Unlocked = false, Progress = 0, Max = 5 },
            new { Title = "地图通才", Description = "在 5 张不同地图获胜", Icon = "🗺️", Unlocked = false, Progress = 0, Max = 5 },
        };

        return Ok(achievements);
    }
}
