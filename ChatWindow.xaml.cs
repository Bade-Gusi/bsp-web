using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
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
        private static readonly string CacheFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat_cache.json");

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

            // 加载历史消息缓存
            try
            {
                if (File.Exists(CacheFile))
                {
                    var cached = JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(File.ReadAllText(CacheFile));
                    if (cached != null)
                    {
                        foreach (var msg in cached)
                        {
                            var sender = msg.TryGetProperty("sender", out var s) ? s.GetString() ?? "玩家" : "玩家";
                            var text = msg.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                            var isMine = msg.TryGetProperty("isMine", out var m) && m.GetBoolean();
                            if (!string.IsNullOrEmpty(text))
                                AddMessage(sender, text, isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left);
                        }
                    }
                }
            }
            catch { }
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
            SaveMessageToCache(sender, text, alignment == HorizontalAlignment.Right);
        }

        private void SaveMessageToCache(string sender, string text, bool isMine)
        {
            try
            {
                var entry = new { sender, text, isMine, time = DateTime.Now.ToString("o") };
                var list = new List<object>();
                if (File.Exists(CacheFile))
                {
                    var json = File.ReadAllText(CacheFile);
                    var existing = JsonSerializer.Deserialize<List<object>>(json);
                    if (existing != null) list = existing;
                }
                list.Add(entry);
                if (list.Count > 200) list.RemoveRange(0, list.Count - 200);
                File.WriteAllText(CacheFile, JsonSerializer.Serialize(list));
            }
            catch { }
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
