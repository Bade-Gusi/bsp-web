namespace BeiShui.ApiGateway.Models;

public class Room
{
    public long Id { get; set; }
    public string RoomCode { get; set; } = "";
    public int GameId { get; set; }
    public long HostUserId { get; set; }
    public int Mode { get; set; } // 0=竞技 1=休闲 2=死斗
    public string MapName { get; set; } = "";
    public int MaxPlayers { get; set; } = 10;
    public int CurrentPlayers { get; set; } = 1;
    public string Password { get; set; } = "";
    public int Status { get; set; } // 0=等待中 1=游戏中 2=已结束
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Game? Game { get; set; }
    public User? HostUser { get; set; }
}
