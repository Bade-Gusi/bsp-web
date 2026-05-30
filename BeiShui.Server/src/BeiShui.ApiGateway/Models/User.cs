namespace BeiShui.ApiGateway.Models;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string? SteamId { get; set; }
    public string? Birthday { get; set; } // MM-dd格式
    public int Status { get; set; } // 0=离线 1=在线 2=游戏中 3=离开
    public int MMR { get; set; } = 1000;
    public int RankId { get; set; } = 1;
    public int WinCount { get; set; }
    public int LoseCount { get; set; }
    public int KillCount { get; set; }
    public int HeadshotCount { get; set; }
    public int TotalGames { get; set; }
    public string? MachineFingerprint { get; set; }
    public string? HardwareInfo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
