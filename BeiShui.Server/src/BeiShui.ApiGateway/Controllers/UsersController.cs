using BeiShui.ApiGateway.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(long id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var rank = await _db.Ranks.FindAsync(user.RankId);
        return Ok(new
        {
            user.Id, user.Username, user.Nickname, user.AvatarUrl,
            user.MMR, RankName = rank?.Name ?? "未定级",
            user.WinCount, user.LoseCount, user.TotalGames, user.CreatedAt
        });
    }

    /// <summary>
    /// 通过 SteamID 查找用户（供 CS2 插件使用）
    /// </summary>
    [HttpGet("steam/{steamId}")]
    public async Task<IActionResult> GetUserBySteamId(string steamId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.SteamId == steamId);
        if (user == null) return NotFound(new { Error = "未绑定" });

        return Ok(new { user.Id, user.Username, user.Nickname, user.SteamId });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { Error = "搜索关键词至少2个字符" });

        var users = await _db.Users
            .Where(u => u.Username.Contains(q) || u.Nickname.Contains(q))
            .Take(20)
            .Select(u => new { u.Id, u.Username, u.Nickname, u.AvatarUrl, u.MMR })
            .ToListAsync();

        return Ok(users);
    }
}
