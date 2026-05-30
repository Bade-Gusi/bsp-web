namespace BeiShui.ApiGateway.Models;

public class Match
{
    public long Id { get; set; }
    public long? RoomId { get; set; }
    public int GameId { get; set; }
    public string MapName { get; set; } = "";
    public int Mode { get; set; }
    public int Status { get; set; } // 0=进行中 1=已结束
    public int WinnerTeam { get; set; } = -1; // -1=未结束
    public int DurationSeconds { get; set; }
    public string ReplayUrl { get; set; } = "";
    public string DemoUrl { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public Game? Game { get; set; }
}
