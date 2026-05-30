using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BeiShui.ApiGateway.Hubs;

[Authorize]
public class MatchHub : Hub
{
    private readonly UserConnectionManager _connectionManager;
    private readonly IConnectionMultiplexer _redis;
    private readonly QuickMatchHandler _quickMatchHandler;
    private readonly IServiceScopeFactory _scopeFactory;

    public MatchHub(
        UserConnectionManager connectionManager,
        IConnectionMultiplexer redis,
        QuickMatchHandler quickMatchHandler,
        IServiceScopeFactory scopeFactory)
    {
        _connectionManager = connectionManager;
        _redis = redis;
        _quickMatchHandler = quickMatchHandler;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// 加入 5v5 匹配队列
    /// </summary>
    public async Task JoinQueue(int gameId, int mode)
    {
        var userId = GetUserId();
        if (userId == 0) return;

        var db = _redis.GetDatabase();

        // 查用户 MMR
        using var scope = _scopeFactory.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await appDb.Users.FindAsync(userId);
        if (user == null) return;

        var key = $"match:5v5:{gameId}:{mode}";

        // 加入 Redis Sorted Set
        await db.SortedSetAddAsync(key, userId.ToString(), user.MMR);
        await db.StringSetAsync($"match:user:{userId}:queue", key, TimeSpan.FromMinutes(30));

        // 加入 SignalR 组
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match:{gameId}:{mode}");

        // 推送当前队列人数
        var queueCount = await db.SortedSetLengthAsync(key);
        await Clients.Caller.SendAsync("OnQueueStatus", new
        {
            GameId = gameId,
            Mode = mode,
            QueueCount = queueCount,
            Status = "searching"
        });

        System.Diagnostics.Debug.WriteLine($"[MatchHub] 用户{userId}加入队列 {key}, 当前队列{queueCount}人");
    }

    /// <summary>
    /// 离开匹配队列
    /// </summary>
    public async Task LeaveQueue(int gameId, int mode)
    {
        var userId = GetUserId();
        if (userId == 0) return;

        var db = _redis.GetDatabase();
        var key = $"match:5v5:{gameId}:{mode}";

        await db.SortedSetRemoveAsync(key, userId.ToString());
        await db.KeyDeleteAsync($"match:user:{userId}:queue");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match:{gameId}:{mode}");

        System.Diagnostics.Debug.WriteLine($"[MatchHub] 用户{userId}离开队列 {key}");
    }

    /// <summary>
    /// 确认匹配
    /// </summary>
    public async Task AcceptMatch(string matchId)
    {
        var userId = GetUserId();
        if (userId == 0) return;

        var allConfirmed = await _quickMatchHandler.ConfirmPlayer(matchId, userId);

        await Clients.Caller.SendAsync("OnMatchAccepted", new
        {
            MatchId = matchId,
            AllConfirmed = allConfirmed
        });
    }

    /// <summary>
    /// 拒绝匹配/取消决斗
    /// </summary>
    public async Task RejectMatch(string matchId, int gameId, int mode)
    {
        var userId = GetUserId();
        await JoinQueue(gameId, mode);

        await Clients.Caller.SendAsync("OnMatchRejected", new
        {
            MatchId = matchId,
            Message = "已重新加入匹配队列"
        });
    }

    /// <summary>
    /// 取消决斗匹配（通知对方）
    /// </summary>
    public async Task CancelDuelMatch(string matchId, long opponentUserId)
    {
        var userId = GetUserId();
        if (userId == 0) return;

        // 通知对方用户已取消
        var opponentConnId = await _connectionManager.GetConnectionId(opponentUserId);
        if (opponentConnId != null)
        {
            await Clients.Client(opponentConnId).SendAsync("OnDuelCancelled", new
            {
                MatchId = matchId,
                Message = "对方取消了匹配"
            });
        }

        await Clients.Caller.SendAsync("OnDuelCancelled", new
        {
            MatchId = matchId,
            Message = "已取消匹配"
        });
    }

    private long GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != 0)
        {
            await _connectionManager.SetConnectionId(userId, Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != 0)
        {
            // 断线时自动从所有队列移除
            var db = _redis.GetDatabase();
            var queueKey = await db.StringGetAsync($"match:user:{userId}:queue");
            if (!queueKey.IsNullOrEmpty)
            {
                await db.SortedSetRemoveAsync(queueKey.ToString(), userId.ToString());
                await db.KeyDeleteAsync($"match:user:{userId}:queue");
            }
            await _connectionManager.RemoveConnection(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
