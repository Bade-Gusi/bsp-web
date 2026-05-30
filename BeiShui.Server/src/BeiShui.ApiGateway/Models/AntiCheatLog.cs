namespace BeiShui.ApiGateway.Models;

public class AntiCheatLog
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? MatchId { get; set; }
    public string AlertType { get; set; } = "";
    public int Severity { get; set; } // 0=低 1=中 2=高
    public string Details { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
