using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class PrivateChatWindow : Window
    {
        private long _toUserId;
        private Action<object>? _onPrivateMessageHandler;

        public PrivateChatWindow() : this(0, "") { }

        public PrivateChatWindow(long toUserId, string toName)
        {
            InitializeComponent();
            _toUserId = toUserId;
            this.Title = $"私聊 - {toName}";

            this.Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));

                // 注册消息回调
                if (App.SignalR != null)
                {
                    _onPrivateMessageHandler = (data) => Dispatcher.Invoke(() => OnPrivateMessage(data));
                    App.SignalR.OnPrivateMessage(_onPrivateMessageHandler);
                }
            };
        }

        private void OnPrivateMessage(object data)
        {
            dynamic? msg = data;
            string fromName = "未知";
            string content = "";
            try { fromName = msg?.FromName ?? "未知"; } catch { }
            try { content = msg?.Content ?? ""; } catch { }

            if (!string.IsNullOrEmpty(content))
            {
                AddBubble(fromName, content, false);
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var text = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || _toUserId <= 0) return;

            if (App.SignalR != null)
            {
                await App.SignalR.SendPrivateMessage(_toUserId, text);
            }

            AddBubble("我", text, true);
            txtMessage.Clear();
        }

        private void AddBubble(string sender, string text, bool isMine)
        {
            var bubble = new Border
            {
                Background = isMine
                    ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                    : new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1A)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 4, 0, 4),
                MaxWidth = 280,
                HorizontalAlignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = isMine
                        ? new SolidColorBrush(Color.FromRgb(0x0A, 0x1A, 0x10))
                        : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                    TextWrapping = TextWrapping.Wrap
                }
            };

            msgPanel.Children.Add(bubble);
            msgScroll.ScrollToEnd();
        }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Send_Click(sender, e);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_onPrivateMessageHandler != null && App.SignalR != null)
                App.SignalR.OnPrivateMessage(_onPrivateMessageHandler);
            this.Close();
        }
    }
}
