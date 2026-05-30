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
    private static readonly List<BroadcastEntry> _broadcastHistory = new();
    private static readonly object _historyLock = new();

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

        var entry = new BroadcastEntry
        {
            ServerAddress = req.ServerAddress,
            AdminName = "管理员",
            Timestamp = DateTime.UtcNow
        };

        lock (_historyLock)
        {
            _broadcastHistory.Insert(0, entry);
            if (_broadcastHistory.Count > 50)
                _broadcastHistory.RemoveRange(50, _broadcastHistory.Count - 50);
        }

        await _hubContext.Clients.All.SendAsync("OnServerBroadcast", new
        {
            entry.ServerAddress,
            entry.AdminName,
            Timestamp = entry.Timestamp.ToString("o")
        });

        return Ok(new { Message = "广播成功" });
    }

    [HttpGet("latest-broadcast")]
    public IActionResult GetLatestBroadcast()
    {
        lock (_historyLock)
        {
            if (_broadcastHistory.Count == 0)
                return NotFound(new { Error = "暂无广播记录" });

            var latest = _broadcastHistory[0];
            return Ok(new
            {
                ServerAddress = latest.ServerAddress,
                BroadcastedAt = latest.Timestamp.ToString("o")
            });
        }
    }

    [HttpGet("broadcasts")]
    public IActionResult GetBroadcasts([FromQuery] int count = 20)
    {
        lock (_historyLock)
        {
            var list = _broadcastHistory.Take(count).Select(e => new
            {
                e.ServerAddress,
                e.AdminName,
                e.Timestamp
            }).ToList();
            return Ok(list);
        }
    }

    public record BroadcastRequest(string ServerAddress, string Password);
    public class BroadcastEntry
    {
        public string ServerAddress { get; set; } = "";
        public string AdminName { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
