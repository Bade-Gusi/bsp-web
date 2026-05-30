using System.Windows;

namespace BeiShuiCS2
{
    public class PlayerSlot
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public bool IsOwner { get; set; }
        public Visibility KickVisibility => (IsOwner && Name != App.CurrentUser?.Username) ? Visibility.Visible : Visibility.Collapsed;
    }
}