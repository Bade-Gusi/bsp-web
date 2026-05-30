using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/holiday")]
public class HolidayController : ControllerBase
{
    private readonly AppDbContext _db;

    public HolidayController(AppDbContext db) => _db = db;

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var today = DateTime.Now.ToString("MM-dd");
        var holiday = await _db.Set<Holiday>()
            .Where(h => h.Date == today && h.IsActive)
            .FirstOrDefaultAsync();
        return Ok(holiday ?? new { });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        var list = await _db.Set<Holiday>()
            .Where(h => h.IsActive)
            .OrderBy(h => h.Date)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("birthday")]
    public async Task<IActionResult> GetBirthday([FromQuery] long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.Birthday)) return Ok(new { IsBirthday = false });

        var today = DateTime.Now.ToString("MM-dd");
        var isBirthday = user.Birthday == today;

        return Ok(new
        {
            IsBirthday = isBirthday,
            Birthday = user.Birthday,
            Message = isBirthday
                ? "今天是您的生日！\n\n愿你在新的一岁里，\n每一枪都精准，每一局都尽兴。\n感谢你选择背水对战平台，\n和千千万万的玩家一起，\n在这里留下属于你的故事。\n\n生日快乐。"
                : ""
        });
    }
}

[ApiController]
[Route("api/welfare")]
public class WelfareController : ControllerBase
{
    private readonly AppDbContext _db;

    public WelfareController(AppDbContext db) => _db = db;

    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        var list = await _db.Set<WelfareItem>()
            .Where(w => w.IsActive)
            .ToListAsync();
        return Ok(list);
    }
}
