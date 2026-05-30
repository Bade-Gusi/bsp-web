namespace BeiShui.ApiGateway.Models;

public class ChatMessage
{
    public long Id { get; set; }
    public long FromUserId { get; set; }
    public long? ToUserId { get; set; }
    public long? RoomId { get; set; }
    public string Content { get; set; } = "";
    public int MsgType { get; set; } // 0=文本 1=图片 2=系统
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? FromUser { get; set; }
}
