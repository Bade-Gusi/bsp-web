using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[Route("api/voice")]
[Authorize]
public class VoiceController : BaseController
{
    private readonly AppDbContext _db;

    public VoiceController(AppDbContext db) => _db = db;

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest req)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        // 检查是否已达上限5个
        var count = await _db.Set<VoiceRoom>().CountAsync(r => r.HostUserId == userId && r.Status == 0);
        if (count >= 5)
            return BadRequest(new { Error = "最多创建5个语音房间" });

        var code = GenerateCode();
        var room = new VoiceRoom
        {
            RoomCode = code,
            Name = req.Name ?? $"{GetUsername()}的语音频道",
            HostUserId = userId,
            Password = req.Password ?? "",
            MaxUsers = req.MaxUsers,
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<VoiceRoom>().Add(room);
        await _db.SaveChangesAsync();

        // 房主自动加入
        _db.Set<VoiceRoomMember>().Add(new VoiceRoomMember { RoomCode = code, UserId = userId });
        await _db.SaveChangesAsync();

        return Ok(new { RoomCode = code, Name = room.Name });
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _db.Set<VoiceRoom>()
            .Include(r => r.HostUser)
            .Where(r => r.Status == 0)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .Select(r => new
            {
                r.RoomCode, r.Name, r.CurrentUsers, r.MaxUsers,
                HasPassword = !string.IsNullOrEmpty(r.Password),
                HostName = r.HostUser!.Nickname,
                r.CreatedAt
            })
            .ToListAsync();
        return Ok(rooms);
    }

    [HttpGet("rooms/mine")]
    public async Task<IActionResult> GetMyRooms()
    {
        var userId = GetUserId();
        var codes = await _db.Set<VoiceRoomMember>()
            .Where(m => m.UserId == userId)
            .Select(m => m.RoomCode)
            .ToListAsync();

        var rooms = await _db.Set<VoiceRoom>()
            .Include(r => r.HostUser)
            .Where(r => codes.Contains(r.RoomCode) && r.Status == 0)
            .Select(r => new
            {
                r.RoomCode, r.Name, r.CurrentUsers, r.MaxUsers,
                HasPassword = !string.IsNullOrEmpty(r.Password),
                HostName = r.HostUser!.Nickname,
                IsHost = r.HostUserId == userId,
                r.CreatedAt
            })
            .ToListAsync();
        return Ok(rooms);
    }

    [HttpPost("rooms/{code}/join")]
    public async Task<IActionResult> JoinRoom(string code, [FromBody] JoinRequest? req)
    {
        var userId = GetUserId();
        var room = await _db.Set<VoiceRoom>().FirstOrDefaultAsync(r => r.RoomCode == code && r.Status == 0);
        if (room == null) return NotFound(new { Error = "房间不存在" });

        if (!string.IsNullOrEmpty(room.Password) && req?.Password != room.Password)
            return BadRequest(new { Error = "密码错误" });

        if (room.CurrentUsers >= room.MaxUsers)
            return BadRequest(new { Error = "房间已满" });

        var exists = await _db.Set<VoiceRoomMember>().AnyAsync(m => m.RoomCode == code && m.UserId == userId);
        if (!exists)
        {
            _db.Set<VoiceRoomMember>().Add(new VoiceRoomMember { RoomCode = code, UserId = userId });
            room.CurrentUsers++;
            await _db.SaveChangesAsync();
        }

        return Ok(new { Message = "已加入" });
    }

    [HttpPost("rooms/{code}/leave")]
    public async Task<IActionResult> LeaveRoom(string code)
    {
        var userId = GetUserId();
        var member = await _db.Set<VoiceRoomMember>().FirstOrDefaultAsync(m => m.RoomCode == code && m.UserId == userId);
        if (member != null)
        {
            _db.Set<VoiceRoomMember>().Remove(member);
            var room = await _db.Set<VoiceRoom>().FirstOrDefaultAsync(r => r.RoomCode == code);
            if (room != null)
            {
                room.CurrentUsers--;
                if (room.HostUserId == userId || room.CurrentUsers <= 0)
                    room.Status = 1;
            }
            await _db.SaveChangesAsync();
        }
        return Ok(new { Message = "已离开" });
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    public record CreateRoomRequest(string? Name, string? Password, int MaxUsers = 10);
    public record JoinRequest(string? Password);
}
