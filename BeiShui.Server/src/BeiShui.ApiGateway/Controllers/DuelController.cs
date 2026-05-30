using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using BeiShui.ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BeiShui.ApiGateway.Controllers;

[Route("api/duel")]
[Authorize]
public class DuelController : BaseController
{
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ServerManagerService _serverManager;

    public DuelController(AppDbContext db, IConnectionMultiplexer redis, ServerManagerService serverManager)
    {
        _db = db;
        _redis = redis;
        _serverManager = serverManager;
    }

    /// <summary>
    /// 向好友发起 1v1 邀约
    /// </summary>
    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] DuelInviteRequest req)
    {
        var fromUserId = GetUserId();
        if (fromUserId == 0) return Unauthorized();
        if (fromUserId == req.ToUserId) return BadRequest(new { Error = "不能邀约自己" });

        // 检查对方是否存在
        var toUser = await _db.Users.FindAsync(req.ToUserId);
        if (toUser == null) return NotFound(new { Error = "用户不存在" });

        // 检查是否有未处理的邀约
        var existing = await _db.DuelInvites.AnyAsync(d =>
            d.FromUserId == fromUserId && d.ToUserId == req.ToUserId && d.Status == 0);
        if (existing) return BadRequest(new { Error = "已有待处理的邀约" });

        var invite = new DuelInvite
        {
            FromUserId = fromUserId,
            ToUserId = req.ToUserId,
            GameId = 1,
            MapName = req.MapName ?? "de_dust2",
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(30)
        };

        _db.DuelInvites.Add(invite);
        await _db.SaveChangesAsync();

        return Ok(new { InviteId = invite.Id, Message = "邀约已发送" });
    }

    /// <summary>
    /// 接受 1v1 邀约
    /// </summary>
    [HttpPost("accept/{inviteId}")]
    public async Task<IActionResult> AcceptInvite(long inviteId)
    {
        var userId = GetUserId();
        var invite = await _db.DuelInvites.Include(i => i.FromUser).FirstOrDefaultAsync(i => i.Id == inviteId);
        if (invite == null) return NotFound();
        if (invite.ToUserId != userId) return Forbid();
        if (invite.Status != 0) return BadRequest(new { Error = "邀约已过期" });
        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            invite.Status = 3;
            await _db.SaveChangesAsync();
            return BadRequest(new { Error = "邀约已超时" });
        }

        invite.Status = 1;
        await _db.SaveChangesAsync();

        // 自动拉起 CS2 服务器
        try
        {
            var fromUser = invite.FromUser;
            var gs = await _serverManager.CreateServer(
                fromUserId: userId,
                hostName: $"{fromUser?.Nickname ?? "玩家"} VS {GetUsername()}",
                mapName: invite.MapName,
                mode: 0,
                maxPlayers: 2,
                password: null
            );

            // 返回服务器地址
            return Ok(new
            {
                gs.RoomCode,
                gs.ServerIp,
                gs.ServerPort,
                ConnectAddress = $"{gs.ServerIp}:{gs.ServerPort}",
                Message = "对战服务器已启动"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = $"启动服务器失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 拒绝邀约
    /// </summary>
    [HttpPost("reject/{inviteId}")]
    public async Task<IActionResult> RejectInvite(long inviteId)
    {
        var userId = GetUserId();
        var invite = await _db.DuelInvites.FindAsync(inviteId);
        if (invite == null) return NotFound();
        if (invite.ToUserId != userId) return Forbid();

        invite.Status = 2;
        await _db.SaveChangesAsync();
        return Ok(new { Message = "已拒绝" });
    }

    /// <summary>
    /// 收到的邀约列表
    /// </summary>
    [HttpGet("invites")]
    public async Task<IActionResult> GetInvites()
    {
        var userId = GetUserId();
        var invites = await _db.DuelInvites
            .Include(d => d.FromUser)
            .Where(d => d.ToUserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(20)
            .Select(d => new
            {
                d.Id,
                FromName = d.FromUser!.Nickname,
                d.MapName,
                d.Status,
                d.CreatedAt,
                d.ExpiresAt
            })
            .ToListAsync();

        return Ok(invites);
    }

    /// <summary>
    /// 加入 1v1 匹配队列
    /// </summary>
    [HttpPost("queue/join")]
    public async Task<IActionResult> JoinQueue()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var db2 = _redis.GetDatabase();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        await db2.SortedSetAddAsync("match:duel:1", userId.ToString(), user.MMR);
        await db2.StringSetAsync($"match:user:{userId}:queue", "match:duel:1", TimeSpan.FromMinutes(10));

        return Ok(new { Message = "已加入匹配队列" });
    }

    /// <summary>
    /// 离开匹配队列
    /// </summary>
    [HttpPost("queue/leave")]
    public async Task<IActionResult> LeaveQueue()
    {
        var userId = GetUserId();
        var db2 = _redis.GetDatabase();

        var queueKey = await db2.StringGetAsync($"match:user:{userId}:queue");
        if (!queueKey.IsNullOrEmpty)
        {
            await db2.SortedSetRemoveAsync(queueKey.ToString(), userId.ToString());
            await db2.KeyDeleteAsync($"match:user:{userId}:queue");
        }

        return Ok(new { Message = "已离开匹配队列" });
    }

    public record DuelInviteRequest(long ToUserId, string? MapName = null);
}
