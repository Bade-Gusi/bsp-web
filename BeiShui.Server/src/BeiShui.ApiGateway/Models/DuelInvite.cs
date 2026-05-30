namespace BeiShui.ApiGateway.Models;

public class DuelInvite
{
    public long Id { get; set; }
    public long FromUserId { get; set; }
    public long ToUserId { get; set; }
    public int GameId { get; set; } = 1;
    public string MapName { get; set; } = "de_dust2";
    public int Status { get; set; } // 0=待接受 1=已接受 2=已拒绝 3=已超时
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public User? FromUser { get; set; }
    public User? ToUser { get; set; }
}
