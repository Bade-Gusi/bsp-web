using System.Collections.ObjectModel;

namespace BeiShuiCS2
{
    public class RoomInfo
    {
        public string RoomId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Map { get; set; } = "";
        public string ServerRegion { get; set; } = "";
        public string Mode { get; set; } = "";
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; } = 10;
        public bool IsOfficial { get; set; }
        public bool IsHidden { get; set; } = false;
        public bool HasPassword { get; set; }
        public string ConnectIP { get; set; } = "";
        public ObservableCollection<RoomPlayer> Players { get; set; } = new();
    }

    public class RoomPlayer
    {
        public string Name { get; set; } = "";
        public string? AvatarPath { get; set; }
        public bool IsOwner { get; set; }
        public string Status { get; set; } = "等待";
    }
}