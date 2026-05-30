namespace BeiShui.ApiGateway.Models;

public class Friend
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long FriendId { get; set; }
    public int Status { get; set; } // 0=待接受 1=已好友
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
    public User? FriendUser { get; set; }
}
