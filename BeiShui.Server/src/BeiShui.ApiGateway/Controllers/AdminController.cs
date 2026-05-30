using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BeiShui.ApiGateway.Hubs;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private const string AdminPassword = "beishui888";
    private readonly IHubContext<BroadcastHub> _hubContext;
    private static string? _latestBroadcast;
    private static DateTime _lastBroadcastTime;

    public AdminController(IHubContext<BroadcastHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest req)
    {
        if (req.Password != AdminPassword)
            return Unauthorized(new { Error = "密码错误" });

        if (string.IsNullOrWhiteSpace(req.ServerAddress))
            return BadRequest(new { Error = "请输入服务器地址" });

        _latestBroadcast = req.ServerAddress;
        _lastBroadcastTime = DateTime.UtcNow;

        var payload = new
        {
            ServerAddress = req.ServerAddress,
            AdminName = "管理员",
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        await _hubContext.Clients.All.SendAsync("OnServerBroadcast", payload);
        Console.WriteLine($"[广播] 服务器地址: {req.ServerAddress}");

        return Ok(new { Message = "广播成功" });
    }

    [HttpGet("latest-broadcast")]
    public IActionResult GetLatestBroadcast()
    {
        if (_latestBroadcast == null)
            return NotFound(new { Error = "暂无广播记录" });

        return Ok(new
        {
            ServerAddress = _latestBroadcast,
            BroadcastedAt = _lastBroadcastTime.ToString("o")
        });
    }

    public record BroadcastRequest(string ServerAddress, string Password);
}
