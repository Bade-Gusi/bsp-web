using StackExchange.Redis;

namespace BeiShui.MatchService;

public class MatchWorker : BackgroundService
{
    private readonly MatchMaker _matchMaker;
    private readonly ILogger<MatchWorker> _logger;

    public MatchWorker(MatchMaker matchMaker, ILogger<MatchWorker> logger)
    {
        _matchMaker = matchMaker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MatchWorker 已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var gameId in new[] { 1, 2, 3, 4 })
                {
                    foreach (var mode in new[] { 0, 1 })
                    {
                        var matches = await _matchMaker.FindMatches(gameId, mode);
                        foreach (var match in matches)
                        {
                            _logger.LogInformation("匹配成功: {Players} (MMR: {AvgMMR})",
                                string.Join(", ", match.Players), match.AvgMMR);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匹配处理异常");
            }

            await Task.Delay(2000, stoppingToken);
        }
    }
}
