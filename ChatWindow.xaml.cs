using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class ChatWindow : Window
    {
        private Action<object>? _onRoomMessageHandler;

        public ChatWindow()
        {
            InitializeComponent();

            AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
            this.Opacity = 0;
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6)));

            // 注册 SignalR 房间消息
            if (App.SignalR != null)
            {
                _onRoomMessageHandler = (data) => Dispatcher.Invoke(() => OnRoomMessage(data));
                App.SignalR.OnRoomMessage(_onRoomMessageHandler);
            }
        }

        private void OnRoomMessage(object data)
        {
            // 解析消息 { fromUserId, fromName, content, timestamp }
            dynamic? msg = data;
            string fromName = "玩家";
            string content = "";
            try { fromName = msg?.FromName ?? "玩家"; } catch { }
            try { content = msg?.Content ?? ""; } catch { }

            if (!string.IsNullOrEmpty(content))
            {
                AddMessage(fromName, content, HorizontalAlignment.Left);
            }
        }

        private void AddMessage(string sender, string text, HorizontalAlignment alignment)
        {
            var wrapper = new Border
            {
                Margin = new Thickness(0, 4, 0, 4),
                HorizontalAlignment = alignment,
                MaxWidth = 300
            };

            // 发送者名字
            if (alignment == HorizontalAlignment.Left)
            {
                var senderText = new TextBlock
                {
                    Text = sender,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                    Margin = new Thickness(4, 0, 0, 2)
                };

                var bubble = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2E, 0x1A)),
                    CornerRadius = new CornerRadius(12, 12, 12, 4),
                    Padding = new Thickness(12, 8, 12, 8),
                    Child = new TextBlock
                    {
                        Text = text,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                        TextWrapping = TextWrapping.Wrap
                    }
                };

                var stack = new StackPanel();
                stack.Children.Add(senderText);
                stack.Children.Add(bubble);
                wrapper.Child = stack;
            }
            else
            {
                var bubble = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),
                    CornerRadius = new CornerRadius(12, 12, 4, 12),
                    Padding = new Thickness(12, 8, 12, 8),
                    Child = new TextBlock
                    {
                        Text = text,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0x1A, 0x10)),
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                wrapper.Child = bubble;
            }

            msgPanel.Children.Add(wrapper);
            msgScroll.ScrollToEnd();
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var text = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // 发送到服务器
            if (App.SignalR != null)
            {
                await App.SignalR.SendRoomMessage("lobby", text);
            }

            AddMessage("我", text, HorizontalAlignment.Right);
            txtMessage.Clear();
        }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Send_Click(sender, e);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_onRoomMessageHandler != null && App.SignalR != null)
                App.SignalR.OnRoomMessage(_onRoomMessageHandler);
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
