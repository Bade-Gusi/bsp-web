using StackExchange.Redis;

namespace BeiShui.ApiGateway.Services;

public class UserConnectionManager
{
    private readonly IDatabase _redis;

    public UserConnectionManager(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task SetConnectionId(long userId, string connectionId)
    {
        await _redis.StringSetAsync($"user:conn:{userId}", connectionId);
        await _redis.KeyExpireAsync($"user:conn:{userId}", TimeSpan.FromMinutes(30));
    }

    public async Task<string?> GetConnectionId(long userId)
    {
        var result = await _redis.StringGetAsync($"user:conn:{userId}");
        return result.IsNullOrEmpty ? null : result.ToString();
    }

    public async Task RemoveConnection(long userId)
    {
        await _redis.KeyDeleteAsync($"user:conn:{userId}");
    }

    public async Task<bool> IsUserOnline(long userId)
    {
        var result = await _redis.StringGetAsync($"user:conn:{userId}");
        return !result.IsNullOrEmpty;
    }

    public async Task<List<long>> GetOnlineFriendIds(long userId, List<long> friendIds)
    {
        var online = new List<long>();
        foreach (var fid in friendIds)
        {
            if (await IsUserOnline(fid))
                online.Add(fid);
        }
        return online;
    }
}
