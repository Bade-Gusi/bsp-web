using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Services;

/// <summary>
/// 语音房间清理服务
/// - 超过180天无人使用的房间自动解散
/// - 空房间（current_users=0）自动释放
/// </summary>
public class VoiceCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VoiceCleanupService> _logger;

    public VoiceCleanupService(IServiceScopeFactory scopeFactory, ILogger<VoiceCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("语音房间清理服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                await CleanupRooms();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "语音房间清理异常");
            }
        }
    }

    private async Task CleanupRooms()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // 超过180天无人使用的房间
        var oldRooms = await db.Set<VoiceRoom>()
            .Where(r => r.Status == 0 && r.CreatedAt < now.AddDays(-180))
            .ToListAsync();

        foreach (var room in oldRooms)
        {
            room.Status = 1;
            _logger.LogInformation("语音房间已到期解散: {Code} (创建于 {CreatedAt})", room.RoomCode, room.CreatedAt);
        }

        // 空房间（当前成员=0 且创建超过7天）
        var emptyRooms = await db.Set<VoiceRoom>()
            .Where(r => r.Status == 0 && r.CurrentUsers <= 0 && r.CreatedAt < now.AddDays(-7))
            .ToListAsync();

        foreach (var room in emptyRooms)
        {
            room.Status = 1;
            _logger.LogInformation("空语音房间已释放: {Code}", room.RoomCode);
        }

        if (oldRooms.Count > 0 || emptyRooms.Count > 0)
            await db.SaveChangesAsync();
    }
}
