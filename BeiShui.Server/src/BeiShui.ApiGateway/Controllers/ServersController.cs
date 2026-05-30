using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using BeiShui.ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[Route("api/servers")]
[Authorize]
public class ServersController : BaseController
{
    private readonly AppDbContext _db;
    private readonly ServerManagerService _serverManager;

    public ServersController(AppDbContext db, ServerManagerService serverManager)
    {
        _db = db;
        _serverManager = serverManager;
    }

    /// <summary>
    /// 申请开服
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateServer([FromBody] CreateServerRequest req)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        try
        {
            var gs = await _serverManager.CreateServer(userId, GetUsername(), req.MapName, req.Mode, req.MaxPlayers, req.Password);
            return Ok(new
            {
                gs.RoomCode,
                gs.ServerIp,
                gs.ServerPort,
                gs.MapName,
                gs.Mode,
                gs.MaxPlayers,
                gs.RconPassword,
                gs.Password,
                ConnectAddress = $"{gs.ServerIp}:{gs.ServerPort}"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = $"开服失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 可加入的服务器列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetServers()
    {
        var servers = await _serverManager.GetJoinableServers();
        return Ok(servers.Select(s => new
        {
            s.RoomCode,
            s.MapName,
            s.Mode,
            s.MaxPlayers,
            HasPassword = !string.IsNullOrEmpty(s.Password),
            HostName = s.HostUser?.Nickname ?? s.HostUser?.Username ?? "",
            s.ServerIp,
            s.ServerPort,
            ConnectAddress = $"{s.ServerIp}:{s.ServerPort}",
            s.CreatedAt
        }));
    }

    /// <summary>
    /// 服务器详情
    /// </summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> GetServer(string code)
    {
        var gs = await _db.GameServers.Include(s => s.HostUser).FirstOrDefaultAsync(s => s.RoomCode == code);
        if (gs == null) return NotFound();
        return Ok(new
        {
            gs.RoomCode,
            gs.MapName,
            gs.Mode,
            gs.MaxPlayers,
            HasPassword = !string.IsNullOrEmpty(gs.Password),
            gs.Status,
            HostName = gs.HostUser?.Nickname ?? "",
            gs.ServerIp,
            gs.ServerPort,
            ConnectAddress = $"{gs.ServerIp}:{gs.ServerPort}",
            gs.CreatedAt
        });
    }

    /// <summary>
    /// 关闭服务器（仅房主）
    /// </summary>
    [HttpDelete("{code}")]
    public async Task<IActionResult> StopServer(string code)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var ok = await _serverManager.StopServer(code, userId);
        if (!ok) return BadRequest(new { Error = "关闭失败" });
        return Ok(new { Message = "服务器已关闭" });
    }

    /// <summary>
    /// 房主心跳
    /// </summary>
    [HttpPost("{code}/heartbeat")]
    public IActionResult Heartbeat(string code)
    {
        _serverManager.Heartbeat(code);
        return Ok(new { Status = "ok" });
    }

    /// <summary>
    /// 加入服务器（返回连接地址）
    /// </summary>
    [HttpPost("{code}/join")]
    public async Task<IActionResult> JoinServer(string code, [FromBody] JoinServerRequest? req)
    {
        var gs = await _db.GameServers.FirstOrDefaultAsync(s => s.RoomCode == code && s.Status == 1);
        if (gs == null) return NotFound(new { Error = "服务器不存在或已关闭" });

        if (!string.IsNullOrEmpty(gs.Password) && req?.Password != gs.Password)
            return BadRequest(new { Error = "密码错误" });

        return Ok(new
        {
            gs.ServerIp,
            gs.ServerPort,
            ConnectAddress = $"{gs.ServerIp}:{gs.ServerPort}",
            gs.RconPassword
        });
    }

    public record CreateServerRequest(string MapName, int Mode = 0, int MaxPlayers = 10, string? Password = null);
    public record JoinServerRequest(string? Password);
}
