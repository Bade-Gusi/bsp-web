namespace BeiShui.ApiGateway.Models;

public class VoiceRoom
{
    public long Id { get; set; }
    public string RoomCode { get; set; } = "";
    public string Name { get; set; } = "";
    public long HostUserId { get; set; }
    public string Password { get; set; } = "";
    public int MaxUsers { get; set; } = 10;
    public int CurrentUsers { get; set; } = 1;
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? HostUser { get; set; }
}

public class VoiceRoomMember
{
    public long Id { get; set; }
    public string RoomCode { get; set; } = "";
    public long UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
