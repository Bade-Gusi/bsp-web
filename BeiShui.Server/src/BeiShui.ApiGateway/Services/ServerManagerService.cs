using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BeiShui.ApiGateway.Services;

/// <summary>
/// CS2 专用服务器(srcds.exe)生命周期管理服务
/// </summary>
public class ServerManagerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ServerManagerService> _logger;
    private readonly Dictionary<long, RunningServer> _runningServers = new();
    private readonly object _lock = new();

    public ServerManagerService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<ServerManagerService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    public class RunningServer
    {
        public long DbId { get; set; }
        public string RoomCode { get; set; } = "";
        public Process? Process { get; set; }
        public int Port { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public int CrashCount { get; set; }
        public string HostName { get; set; } = "";
    }

    private string SrcdsPath => _config["Cs2Server:SrcdsPath"] ?? @"C:\steamcmd\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\srcds.exe";
    private int MinPort => _config.GetValue<int>("Cs2Server:MinPort", 27015);
    private int MaxPort => _config.GetValue<int>("Cs2Server:MaxPort", 27215);
    private int HeartbeatTimeoutSeconds => _config.GetValue<int>("Cs2Server:HeartbeatTimeoutSeconds", 90);

    /// <summary>
    /// 创建并启动 CS2 服务器
    /// </summary>
    public async Task<GameServer> CreateServer(long hostUserId, string hostName, string mapName, int mode, int maxPlayers, string? password)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hostUser = await db.Users.FindAsync(hostUserId);
        if (hostUser == null) throw new ArgumentException("用户不存在");

        var port = FindAvailablePort();
        var roomCode = GenerateRoomCode();
        var rconPwd = GenerateRconPassword();

        var gameServer = new GameServer
        {
            RoomCode = roomCode,
            HostUserId = hostUserId,
            MapName = mapName,
            Mode = mode,
            MaxPlayers = maxPlayers,
            Password = password ?? "",
            ServerIp = GetLocalIpAddress(),
            ServerPort = port,
            RconPassword = rconPwd,
            Status = 0,
            CreatedAt = DateTime.UtcNow
        };

        db.GameServers.Add(gameServer);
        await db.SaveChangesAsync();

        // 启动 srcds 进程
        var psi = new ProcessStartInfo
        {
            FileName = SrcdsPath,
            Arguments = BuildSrcdsArgs(roomCode, mapName, mode, maxPlayers, port, password, rconPwd, hostName),
            UseShellExecute = false,
            CreateNoWindow = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };

        try
        {
            var proc = Process.Start(psi);
            if (proc != null)
            {
                gameServer.ProcessId = proc.Id;
                gameServer.Status = 1;
                gameServer.StartedAt = DateTime.UtcNow;

                lock (_lock)
                {
                    _runningServers[gameServer.Id] = new RunningServer
                    {
                        DbId = gameServer.Id,
                        RoomCode = roomCode,
                        Process = proc,
                        Port = port,
                        StartedAt = DateTime.UtcNow,
                        LastHeartbeat = DateTime.UtcNow,
                        HostName = hostName
                    };
                }

                db.GameServers.Update(gameServer);
                await db.SaveChangesAsync();

                _logger.LogInformation("CS2 服务器已启动: {Code} (PID={Pid}, Port={Port})", roomCode, proc.Id, port);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动 srcds 失败: {Path}", SrcdsPath);
            gameServer.Status = 2;
            gameServer.EndedAt = DateTime.UtcNow;
            db.GameServers.Update(gameServer);
            await db.SaveChangesAsync();
            throw;
        }

        return gameServer;
    }

    /// <summary>
    /// 关闭并清理服务器
    /// </summary>
    public async Task<bool> StopServer(string roomCode, long userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var gs = await db.GameServers.FirstOrDefaultAsync(s => s.RoomCode == roomCode);
        if (gs == null) return false;
        if (gs.HostUserId != userId) return false; // 仅房主可关

        KillProcess(gs.Id);
        gs.Status = 2;
        gs.EndedAt = DateTime.UtcNow;
        db.GameServers.Update(gs);
        await db.SaveChangesAsync();

        _logger.LogInformation("CS2 服务器已关闭: {Code}", roomCode);
        return true;
    }

    /// <summary>
    /// 房主心跳（客户端每30秒调用）
    /// </summary>
    public void Heartbeat(string roomCode)
    {
        lock (_lock)
        {
            var entry = _runningServers.Values.FirstOrDefault(s => s.RoomCode == roomCode);
            if (entry != null)
            {
                entry.LastHeartbeat = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// 获取可加入的服务器列表
    /// </summary>
    public async Task<List<GameServer>> GetJoinableServers()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.GameServers
            .Include(s => s.HostUser)
            .Where(s => s.Status == 1 && s.MaxPlayers > 0)
            .OrderByDescending(s => s.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    /// <summary>
    /// 后台循环：检查进程存活性 + 心跳超时
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServerManagerService 已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, stoppingToken);
                CheckProcesses();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "服务器监控异常");
            }
        }

        // 退出时关闭所有 Srcds
        lock (_lock)
        {
            foreach (var entry in _runningServers.Values)
            {
                KillProcessInternal(entry);
            }
            _runningServers.Clear();
        }
    }

    private void CheckProcesses()
    {
        lock (_lock)
        {
            var deadKeys = new List<long>();
            var now = DateTime.UtcNow;

            foreach (var kv in _runningServers)
            {
                var entry = kv.Value;

                // 检查心跳超时
                if ((now - entry.LastHeartbeat).TotalSeconds > HeartbeatTimeoutSeconds)
                {
                    _logger.LogWarning("房主心跳超时，关闭服务器: {Code}", entry.RoomCode);
                    KillProcessInternal(entry);
                    deadKeys.Add(kv.Key);
                    continue;
                }

                // 检查进程是否存活
                if (entry.Process != null)
                {
                    try
                    {
                        if (entry.Process.HasExited)
                        {
                            entry.CrashCount++;
                            if (entry.CrashCount >= 3)
                            {
                                _logger.LogWarning("服务器崩溃3次，不再重启: {Code}", entry.RoomCode);
                                KillProcessInternal(entry);
                                deadKeys.Add(kv.Key);
                            }
                            else
                            {
                                _logger.LogWarning("服务器进程退出，自动重启(第{Count}次): {Code}", entry.CrashCount, entry.RoomCode);
                                RestartProcess(entry);
                            }
                        }
                    }
                    catch { }
                }
            }

            // 清理并更新数据库
            foreach (var id in deadKeys)
            {
                if (_runningServers.TryGetValue(id, out var entry))
                {
                    _ = UpdateDbStatusAsync(entry.DbId, 2);
                    _runningServers.Remove(id);
                }
            }
        }
    }

    private void RestartProcess(RunningServer entry)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = SrcdsPath,
                Arguments = BuildSrcdsArgs(entry.RoomCode, "", 0, 10, entry.Port, "", "", entry.HostName),
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                entry.Process = proc;
                entry.StartedAt = DateTime.UtcNow;
                entry.LastHeartbeat = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启 srcds 失败");
        }
    }

    private static void KillProcessInternal(RunningServer entry)
    {
        if (entry.Process != null)
        {
            try
            {
                if (!entry.Process.HasExited)
                {
                    entry.Process.Kill(entireProcessTree: true);
                    entry.Process.WaitForExit(5000);
                }
            }
            catch { }
            entry.Process.Dispose();
            entry.Process = null;
        }
    }

    private void KillProcess(long serverDbId)
    {
        lock (_lock)
        {
            if (_runningServers.TryGetValue(serverDbId, out var entry))
            {
                KillProcessInternal(entry);
                _runningServers.Remove(serverDbId);
            }
        }
    }

    private async Task UpdateDbStatusAsync(long dbId, int status)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gs = await db.GameServers.FindAsync(dbId);
        if (gs != null)
        {
            gs.Status = status;
            gs.EndedAt = DateTime.UtcNow;
            db.GameServers.Update(gs);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 构建 srcds 启动参数
    /// </summary>
    private string BuildSrcdsArgs(string roomCode, string mapName, int mode, int maxPlayers, int port, string? password, string rconPwd, string hostName)
    {
        var gameType = mode switch { 1 => "casual", 2 => "deathmatch", _ => "competitive" };
        var pwdArg = string.IsNullOrEmpty(password) ? "" : $"+sv_password {password}";

        return $"-game cs2 -console -usercon " +
               $"+map {mapName} +maxplayers {maxPlayers} " +
               $"-port {port} {pwdArg} " +
               $"+rcon_password {rconPwd} " +
               $"-ip 0.0.0.0 " +
               $"+hostname \"背水对战 - {hostName}的房间\" " +
               $"+sv_visiblemaxplayers {maxPlayers} " +
               $"+game_type {gameType}";
    }

    /// <summary>
    /// 查找可用端口
    /// </summary>
    private int FindAvailablePort()
    {
        lock (_lock)
        {
            var usedPorts = _runningServers.Values.Select(s => s.Port).ToHashSet();

            // 也检查系统占用
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var activeListeners = ipProperties.GetActiveTcpListeners();
            foreach (var listener in activeListeners)
                usedPorts.Add(listener.Port);

            for (int port = MinPort; port <= MaxPort; port++)
            {
                if (!usedPorts.Contains(port))
                    return port;
            }
            throw new InvalidOperationException("没有可用端口");
        }
    }

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
        return ip?.ToString() ?? "127.0.0.1";
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private static string GenerateRconPassword()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 16).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
