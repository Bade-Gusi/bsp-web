using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MessagesController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取最近的聊天消息（大厅消息，最近100条）
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMessages([FromQuery] int count = 100)
    {
        var userId = GetUserId();

        var messages = await _db.ChatMessages
            .Where(m => m.RoomId == null)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .Select(m => new
            {
                m.Id,
                FromName = m.FromUser!.Nickname,
                m.Content,
                m.CreatedAt,
                IsMine = m.FromUserId == userId
            })
            .ToListAsync();

        return Ok(messages.OrderBy(m => m.CreatedAt));
    }

    /// <summary>
    /// 获取私聊消息
    /// </summary>
    [HttpGet("private/{friendId}")]
    [Authorize]
    public async Task<IActionResult> GetPrivateMessages(long friendId, [FromQuery] int count = 50)
    {
        var userId = GetUserId();

        var messages = await _db.ChatMessages
            .Where(m => (m.FromUserId == userId && m.ToUserId == friendId) ||
                        (m.FromUserId == friendId && m.ToUserId == userId))
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .Select(m => new
            {
                m.Id,
                FromName = m.FromUser!.Nickname,
                m.Content,
                m.CreatedAt,
                IsMine = m.FromUserId == userId
            })
            .ToListAsync();

        return Ok(messages.OrderBy(m => m.CreatedAt));
    }

    private long GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? long.Parse(claim.Value) : 0;
    }
}
