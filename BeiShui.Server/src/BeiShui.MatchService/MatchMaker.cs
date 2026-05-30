using StackExchange.Redis;

namespace BeiShui.MatchService;

public class MatchGroup
{
    public long[] Players { get; set; } = Array.Empty<long>();
    public int AvgMMR { get; set; }
}

public class MatchMaker
{
    private readonly IDatabase _redis;

    public MatchMaker(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    // Redis Key 格式: match:queue:{gameId}:{mode}
    // Sorted Set: member=userId, score=mmr

    public async Task JoinQueue(long userId, int gameId, int mode, int mmr)
    {
        var key = $"match:queue:{gameId}:{mode}";
        await _redis.SortedSetAddAsync(key, userId.ToString(), mmr);
        await _redis.StringSetAsync($"match:user:{userId}:queue", key, TimeSpan.FromMinutes(30));
    }

    public async Task LeaveQueue(long userId)
    {
        var queueKey = await _redis.StringGetAsync($"match:user:{userId}:queue");
        if (!queueKey.IsNullOrEmpty)
        {
            await _redis.SortedSetRemoveAsync(queueKey.ToString(), userId.ToString());
            await _redis.KeyDeleteAsync($"match:user:{userId}:queue");
        }
    }

    public async Task<List<MatchGroup>> FindMatches(int gameId, int mode)
    {
        var key = $"match:queue:{gameId}:{mode}";
        var matches = new List<MatchGroup>();

        var players = await _redis.SortedSetRangeByRankWithScoresAsync(key, 0, -1);
        if (players.Length < 2) return matches;

        var playerList = players.Select(p => new
        {
            UserId = (long)p.Element,
            MMR = (int)p.Score
        }).ToList();

        // 贪心配对：按 MMR 最接近配对
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

            if (bestJ != -1 && bestDiff <= 200) // MMR 差距不超过 200
            {
                used.Add(playerList[i].UserId);
                used.Add(playerList[bestJ].UserId);
                matches.Add(new MatchGroup
                {
                    Players = new[] { playerList[i].UserId, playerList[bestJ].UserId },
                    AvgMMR = (playerList[i].MMR + playerList[bestJ].MMR) / 2
                });
            }
        }

        // 从 Redis 移除已配对的玩家
        foreach (var m in matches)
        {
            foreach (var uid in m.Players)
            {
                await _redis.SortedSetRemoveAsync(key, uid.ToString());
                await _redis.KeyDeleteAsync($"match:user:{uid}:queue");
            }
        }

        return matches;
    }
}

/// <summary>
/// MMR 变化计算 - 基于 Elo 算法变体
/// </summary>
public class MatchResultProcessor
{
    public int CalculateMMRChange(int playerMmr, int opponentMmr, bool isWinner)
    {
        double expected = 1.0 / (1.0 + Math.Pow(10, (opponentMmr - playerMmr) / 400.0));
        double actual = isWinner ? 1.0 : 0.0;

        int k = playerMmr switch
        {
            < 500 => 32,    // 青铜
            < 1500 => 24,   // 白银-黄金
            < 2500 => 16,   // 铂金-钻石
            _ => 12,        // 大师以上
        };

        int change = (int)(k * (actual - expected));
        return Math.Max(change, -40);
    }
}
