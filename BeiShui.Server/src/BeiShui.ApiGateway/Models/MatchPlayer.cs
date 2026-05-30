namespace BeiShui.ApiGateway.Models;

public class MatchPlayer
{
    public long Id { get; set; }
    public long MatchId { get; set; }
    public long UserId { get; set; }
    public int Team { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int Headshots { get; set; }
    public int Damage { get; set; }
    public int MVPs { get; set; }
    public int Score { get; set; }
    public int MMRChange { get; set; }
    public bool IsWinner { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Match? Match { get; set; }
    public User? User { get; set; }
}
