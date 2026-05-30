using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BeiShui.AntiCheat;

[ApiController]
[Route("api/anticheat")]
public class AntiCheatController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AntiCheatStore _store;

    // 内存限流
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimits = new();
    private const int MAX_REQUESTS_PER_MINUTE = 30;

    public AntiCheatController(IConfiguration config, AntiCheatStore store)
    {
        _config = config;
        _store = store;
    }

    private string GetSecret() =>
        _config["AntiCheat:Secret"] ?? "ChangeThisSecretInProduction!";

    /// <summary>
    /// 心跳上报 — 客户端定期调用
    /// </summary>
    [HttpPost("heartbeat")]
    public IActionResult ReceiveHeartbeat([FromBody] HeartbeatRequest req)
    {
        if (req == null)
            return BadRequest(new { Error = "无效心跳" });

        // 限流
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!CheckRateLimit(clientIp))
            return StatusCode(429, new { Error = "请求过于频繁" });

        // 签名验证
        if (!VerifySignature(req))
            return Unauthorized(new { Error = "签名验证失败" });

        // 时间窗口验证（60秒）
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - req.Timestamp) > 60)
            return BadRequest(new { Error = "心跳过期" });

        return Ok(new { Status = "ok", ServerTime = now });
    }

    /// <summary>
    /// 违规上报
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> ReportViolation([FromBody] ViolationReport report)
    {
        if (report?.Alerts == null || report.Alerts.Count == 0)
            return BadRequest(new { Error = "无效报告" });

        // 记录到 JSON 文件
        await _store.SaveViolationsAsync(report.Alerts);

        return Ok(new { Message = "已记录", AlertCount = report.Alerts.Count });
    }

    /// <summary>
    /// 查询违规记录（最近100条）
    /// </summary>
    [HttpGet("violations")]
    public async Task<IActionResult> GetViolations([FromQuery] int count = 100)
    {
        var violations = await _store.GetRecentViolationsAsync(count);
        return Ok(violations);
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "healthy",
            Version = "3.0.0",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            TotalViolations = _store.GetViolationCount()
        });
    }

    private bool VerifySignature(HeartbeatRequest req)
    {
        var secret = GetSecret();
        var data = $"{req.Timestamp}|{req.Nonce}|{req.GameRunning}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return req.Signature == Convert.ToBase64String(hash);
    }

    private static bool CheckRateLimit(string clientIp)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var entry = _rateLimits.GetOrAdd(clientIp, _ => new RateLimitEntry());

        lock (entry)
        {
            if (now - entry.WindowStart >= 60)
            {
                entry.WindowStart = now;
                entry.Count = 0;
            }

            entry.Count++;
            return entry.Count <= MAX_REQUESTS_PER_MINUTE;
        }
    }

    private class RateLimitEntry
    {
        public long WindowStart { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public int Count { get; set; }
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
