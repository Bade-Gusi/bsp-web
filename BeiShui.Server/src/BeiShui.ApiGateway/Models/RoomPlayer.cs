namespace BeiShui.ApiGateway.Models;

public class RoomPlayer
{
    public long Id { get; set; }
    public long RoomId { get; set; }
    public long UserId { get; set; }
    public int Team { get; set; } // 0=CT/蓝队 1=T/红队
    public int Slot { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Room? Room { get; set; }
    public User? User { get; set; }
}
