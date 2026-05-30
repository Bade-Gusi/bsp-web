using System.Windows;
using System.Windows.Media;

namespace BeiShuiCS2
{
    public class PrivateMessage
    {
        public string Text { get; set; } = "";
        public bool IsFromMe { get; set; }
        public HorizontalAlignment Alignment => IsFromMe ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        public Brush BubbleColor => IsFromMe ? new SolidColorBrush(Color.FromRgb(74, 222, 128)) : Brushes.LightGray;
    }
}