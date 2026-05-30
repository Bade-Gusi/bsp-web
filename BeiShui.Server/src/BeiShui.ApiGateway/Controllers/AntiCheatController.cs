using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace BeiShui.ApiGateway.Controllers;

[ApiController]
[Route("api/anticheat")]
public class AntiCheatController : ControllerBase
{
    private readonly IConfiguration _config;

    public AntiCheatController(IConfiguration config) => _config = config;

    private string GetSecret() => _config["AntiCheat:Secret"] ?? "BeiShuiDefaultAntiCheatSecret_ChangeInProduction!";

    [HttpPost("heartbeat")]
    public IActionResult ReceiveHeartbeat([FromBody] HeartbeatRequest req)
    {
        if (req == null) return BadRequest(new { Error = "无效心跳" });
        if (!VerifySignature(req)) return Unauthorized(new { Error = "签名验证失败" });

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - req.Timestamp) > 30) return BadRequest(new { Error = "心跳过期" });

        return Ok(new { Status = "ok", ServerTime = now });
    }

    [HttpPost("report")]
    public IActionResult ReportViolation([FromBody] ViolationReport report)
    {
        if (report?.Alerts == null || report.Alerts.Count == 0)
            return BadRequest(new { Error = "无效报告" });

        foreach (var alert in report.Alerts)
            System.Console.WriteLine($"[AntiCheat] 违规报告: {alert}");

        return Ok(new { Message = "已记录", AlertCount = report.Alerts.Count });
    }

    private bool VerifySignature(HeartbeatRequest req)
    {
        var secret = GetSecret();
        var data = $"{req.Timestamp}|{req.Nonce}|{req.GameRunning}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return req.Signature == Convert.ToBase64String(hash);
    }

    public class HeartbeatRequest
    {
        public long Timestamp { get; set; }
        public bool GameRunning { get; set; }
        public long Nonce { get; set; }
        public string Signature { get; set; } = "";
    }

    public class ViolationReport
    {
        public List<string> Alerts { get; set; } = new();
    }
}
