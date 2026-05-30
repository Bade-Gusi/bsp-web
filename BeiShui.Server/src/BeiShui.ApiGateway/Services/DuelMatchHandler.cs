using Microsoft.AspNetCore.SignalR;
using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Hubs;
using BeiShui.ApiGateway.Models;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Services;

/// <summary>
/// 1v1 匹配处理后台服务
/// 每 2 秒扫描 Redis 队列，按 MMR 配对
/// </summary>
public class DuelMatchHandler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<MatchHub> _hubContext;
    private readonly ILogger<DuelMatchHandler> _logger;

    public DuelMatchHandler(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        IHubContext<MatchHub> hubContext,
        ILogger<DuelMatchHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DuelMatchHandler 已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDuelQueue();
                await ProcessExpiredInvites();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "1v1 匹配处理异常");
            }

            await Task.Delay(2000, stoppingToken);
        }
    }

    private async Task ProcessDuelQueue()
    {
        var db = _redis.GetDatabase();
        const string key = "match:duel:1";

        var players = await db.SortedSetRangeByRankWithScoresAsync(key, 0, -1);
        if (players.Length < 2) return;

        var playerList = players.Select(p => (UserId: (long)p.Element, MMR: (int)p.Score)).ToList();
        var used = new HashSet<long>();

        for (int i = 0; i < playerList.Count - 1; i++)
        {
            if (used.Contains(playerList[i].UserId)) continue;

            int bestJ = -1;
            int bestDiff = int.MaxValue;

            for (int j = i + 1; j < playerList.Count; j++)
            {
                if (used.Contains(playerList[j].UserId)) continue;
                int diff = Math.Abs(playerList[i].MMR - playerList[j].MMR);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestJ = j;
                }
            }

            if (bestJ != -1 && bestDiff <= 300)
            {
                used.Add(playerList[i].UserId);
                used.Add(playerList[bestJ].UserId);

                // 从队列移除
                await db.SortedSetRemoveAsync(key, new RedisValue[] { playerList[i].UserId.ToString(), playerList[bestJ].UserId.ToString() });
                await db.KeyDeleteAsync($"match:user:{playerList[i].UserId}:queue");
                await db.KeyDeleteAsync($"match:user:{playerList[bestJ].UserId}:queue");

                // 查用户信息
                using var scope = _scopeFactory.CreateScope();
                var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userA = await appDb.Users.FindAsync(playerList[i].UserId);
                var userB = await appDb.Users.FindAsync(playerList[bestJ].UserId);

                // 通过 SignalR 通知双方
                await NotifyDuelMatch(playerList[i].UserId, playerList[bestJ].UserId, userA, userB);
            }
        }
    }

    private async Task NotifyDuelMatch(long userIdA, long userIdB, User? userA, User? userB)
    {
        var connManager = _hubContext.Clients;

        // 找连接
        using var scope = _scopeFactory.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var guestConns = await appDb.Users.Where(u => u.Id == userIdA || u.Id == userIdB).ToListAsync();

        await connManager.User(userIdA.ToString()).SendAsync("OnDuelMatchFound", new
        {
            OpponentName = userB?.Nickname ?? userB?.Username ?? "未知",
            OpponentMMR = userB?.MMR ?? 0,
            MatchId = $"{userIdA}_{userIdB}_{DateTime.UtcNow.Ticks}"
        });

        await connManager.User(userIdB.ToString()).SendAsync("OnDuelMatchFound", new
        {
            OpponentName = userA?.Nickname ?? userA?.Username ?? "未知",
            OpponentMMR = userA?.MMR ?? 0,
            MatchId = $"{userIdA}_{userIdB}_{DateTime.UtcNow.Ticks}"
        });

        _logger.LogInformation("1v1匹配成功: {A} vs {B}", userIdA, userIdB);
    }

    private async Task ProcessExpiredInvites()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expired = await db.DuelInvites
            .Where(d => d.Status == 0 && d.ExpiresAt < DateTime.UtcNow)
            .Take(50)
            .ToListAsync();

        foreach (var invite in expired)
        {
            invite.Status = 3;
        }

        if (expired.Count > 0)
            await db.SaveChangesAsync();
    }
}
