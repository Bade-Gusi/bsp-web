namespace BeiShui.ApiGateway.Models;

public class GameServer
{
    public long Id { get; set; }
    public string RoomCode { get; set; } = "";
    public long HostUserId { get; set; }
    public int GameId { get; set; } = 1;
    public string MapName { get; set; } = "de_dust2";
    public int Mode { get; set; } // 0=竞技 1=休闲 2=死斗
    public int MaxPlayers { get; set; } = 10;
    public string Password { get; set; } = "";
    public string ServerIp { get; set; } = "";
    public int ServerPort { get; set; } = 27015;
    public string RconPassword { get; set; } = "";
    public int ProcessId { get; set; }
    public int Status { get; set; } // 0=启动中 1=运行中 2=已结束
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public int CrashCount { get; set; }

    public User? HostUser { get; set; }
}
