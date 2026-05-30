using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[Route("api/rooms")]
[Authorize]
public class RoomsController : BaseController
{
    private readonly AppDbContext _db;

    public RoomsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetRooms([FromQuery] int gameId = 0, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var query = _db.Rooms.Include(r => r.HostUser).Where(r => r.Status == 0);
        if (gameId > 0) query = query.Where(r => r.GameId == gameId);

        var rooms = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(r => new
            {
                r.RoomCode,
                r.MapName,
                r.Mode,
                r.MaxPlayers,
                r.CurrentPlayers,
                HasPassword = !string.IsNullOrEmpty(r.Password),
                HostName = r.HostUser!.Nickname,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetRoom(string code)
    {
        var room = await _db.Rooms.Include(r => r.HostUser)
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.RoomCode == code);
        if (room == null) return NotFound();

        var players = await _db.RoomPlayers.Include(rp => rp.User)
            .Where(rp => rp.RoomId == room.Id)
            .ToListAsync();

        return Ok(new
        {
            room.RoomCode, room.MapName, room.Mode, room.MaxPlayers, room.CurrentPlayers,
            HasPassword = !string.IsNullOrEmpty(room.Password),
            room.Status,
            HostName = room.HostUser?.Nickname,
            GameName = room.Game?.Name,
            Players = players.Select(p => new { p.User?.Nickname, p.Team, p.Slot })
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest req)
    {
        var userId = GetUserId();
        var code = GenerateRoomCode();

        var room = new Room
        {
            RoomCode = code,
            GameId = req.GameId,
            HostUserId = userId,
            Mode = req.Mode,
            MapName = req.MapName ?? "",
            MaxPlayers = req.MaxPlayers,
            Password = req.Password ?? "",
            CurrentPlayers = 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        // 房主自动加入
        _db.RoomPlayers.Add(new RoomPlayer
        {
            RoomId = room.Id, UserId = userId, Team = 0, Slot = 0
        });
        await _db.SaveChangesAsync();

        return Ok(new { RoomCode = code });
    }

    [HttpPost("{code}/join")]
    public async Task<IActionResult> JoinRoom(string code, [FromBody] JoinRoomRequest? req)
    {
        var userId = GetUserId();
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomCode == code && r.Status == 0);
        if (room == null) return NotFound(new { Error = "房间不存在或已结束" });

        if (!string.IsNullOrEmpty(room.Password) && (req?.Password != room.Password))
            return BadRequest(new { Error = "密码错误" });

        if (room.CurrentPlayers >= room.MaxPlayers)
            return BadRequest(new { Error = "房间已满" });

        if (await _db.RoomPlayers.AnyAsync(rp => rp.RoomId == room.Id && rp.UserId == userId))
            return BadRequest(new { Error = "已在房间中" });

        _db.RoomPlayers.Add(new RoomPlayer { RoomId = room.Id, UserId = userId });
        room.CurrentPlayers++;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "加入成功" });
    }

    [HttpPost("{code}/leave")]
    public async Task<IActionResult> LeaveRoom(string code)
    {
        var userId = GetUserId();
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomCode == code);
        if (room == null) return NotFound();

        var rp = await _db.RoomPlayers.FirstOrDefaultAsync(rp => rp.RoomId == room.Id && rp.UserId == userId);
        if (rp == null) return BadRequest(new { Error = "不在房间中" });

        _db.RoomPlayers.Remove(rp);
        room.CurrentPlayers--;

        // 房主离开则解散房间
        if (room.HostUserId == userId)
            room.Status = 2;

        await _db.SaveChangesAsync();
        return Ok(new { Message = "已离开" });
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    public record CreateRoomRequest(int GameId, int Mode, string? MapName, int MaxPlayers = 10, string? Password = null);
    public record JoinRoomRequest(string? Password);
}
