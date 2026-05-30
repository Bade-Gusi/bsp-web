using System.Text.Json.Serialization;
using System.Windows.Media;

namespace BeiShuiCS2
{
    public class Friend
    {
        public int FriendId { get; set; }
        public string Name { get; set; } = "";
        public int ELO { get; set; }
        public bool IsOnline { get; set; }
        public string? SteamID { get; set; }
        public string? AvatarPath { get; set; }

        public string StatusDisplay => IsOnline ? "在线" : "离线";

        [JsonIgnore]
        public Brush StatusColor => IsOnline ? new SolidColorBrush(Color.FromRgb(74, 222, 128)) : Brushes.Gray;
    }
}