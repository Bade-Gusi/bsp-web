using System.Collections.Concurrent;

namespace BeiShui.ApiGateway.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RateEntry> _clients = new();

    // 不同接口的频率限制
    private static readonly Dictionary<string, int> _limits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/api/auth/login"] = 10,           // 登录: 10次/分钟
        ["/api/auth/register"] = 5,         // 注册: 5次/分钟
        ["/api/auth/forgot-password"] = 3,  // 忘记密码: 3次/分钟
        ["/api/admin/broadcast"] = 6,       // 广播: 6次/分钟
    };
    private const int DefaultLimit = 60;    // 默认: 60次/分钟

    public RateLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{ip}:{path}";

        var limit = _limits.FirstOrDefault(kv => path.StartsWith(kv.Key)).Value;
        if (limit == 0) limit = DefaultLimit;

        var entry = _clients.GetOrAdd(key, _ => new RateEntry());

        lock (entry)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - entry.WindowStart >= 60)
            {
                entry.WindowStart = now;
                entry.Count = 0;
            }
            entry.Count++;

            if (entry.Count > limit)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                context.Response.WriteAsJsonAsync(new { error = "请求过于频繁，请稍后再试" });
                return;
            }
        }

        await _next(context);
    }

    private class RateEntry
    {
        public long WindowStart { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public int Count { get; set; }
    }
}
