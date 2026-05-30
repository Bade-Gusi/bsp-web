using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Windows.Threading;

namespace BeiShuiCS2.Services
{
    public class SignalRService
    {
        private readonly Dictionary<string, HubConnection> _connections = new();
        private readonly Dispatcher _dispatcher;
        private string? _token;

        public bool IsConnected { get; private set; }
        public event Action<bool>? OnConnectionChanged;

        public SignalRService()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public async Task ConnectAsync(string token)
        {
            _token = token;
            var baseUrl = NetworkHelper.BuildHubBaseUrl();

            // 文档方案：连接多个 Hub
            var hubs = new[] { "match", "game", "chat", "broadcast" };
            foreach (var hub in hubs)
            {
                var hubUrl = $"{baseUrl}/hubs/{hub}";
                var connection = CreateConnection(hubUrl, token);
                _connections[hub] = connection;
            }

            // 并行启动所有 Hub 连接
            var tasks = new List<Task>();
            foreach (var kv in _connections)
            {
                tasks.Add(ConnectWithRetryAsync(kv.Value, kv.Key));
            }
            await Task.WhenAll(tasks);

            IsConnected = true;
            _dispatcher.Invoke(() => OnConnectionChanged?.Invoke(true));
        }

        private HubConnection CreateConnection(string hubUrl, string token)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    options.Headers["Authorization"] = $"Bearer {token}";
                    if (hubUrl.StartsWith("https"))
                        options.HttpMessageHandlerFactory = _ => new System.Net.Http.HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                        };
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) })
                .Build();

            connection.Closed += async (error) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR-{hubUrl}] 连接关闭: {error?.Message}");
                await Task.Delay(5000);
                try { await connection.StartAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SignalR] 重连失败: {ex.Message}"); }
            };

            return connection;
        }

        private async Task ConnectWithRetryAsync(HubConnection connection, string hubName, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await connection.StartAsync();
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Hub '{hubName}' 连接成功");
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Hub '{hubName}' 连接失败(尝试{i+1}): {ex.Message}");
                    if (i < maxRetries - 1)
                        await Task.Delay(1000 * (i + 1));
                }
            }
        }

        private HubConnection? GetHub(string name)
        {
            _connections.TryGetValue(name, out var conn);
            return conn;
        }

        private bool IsHubConnected(string name)
        {
            return GetHub(name)?.State == HubConnectionState.Connected;
        }

        public async Task DisconnectAsync()
        {
            foreach (var kv in _connections)
            {
                try
                {
                    await kv.Value.StopAsync();
                    await kv.Value.DisposeAsync();
                }
                catch { }
            }
            _connections.Clear();
            IsConnected = false;
        }

        // ==================== Match Hub ====================
        public async Task JoinQueue(int gameId, int mode)
        {
            if (IsHubConnected("match"))
                await GetHub("match")!.InvokeAsync("JoinQueue", gameId, mode);
        }
        public async Task LeaveQueue(int gameId, int mode)
        {
            if (IsHubConnected("match"))
                await GetHub("match")!.InvokeAsync("LeaveQueue", gameId, mode);
        }
        public async Task AcceptMatch(string matchId)
        {
            if (IsHubConnected("match"))
                await GetHub("match")!.InvokeAsync("AcceptMatch", matchId);
        }
        public async Task RejectMatch(string matchId, int gameId, int mode)
        {
            if (IsHubConnected("match"))
                await GetHub("match")!.InvokeAsync("RejectMatch", matchId, gameId, mode);
        }
        public void OnMatchFound(Action<object> handler) => GetHub("match")?.On("OnMatchFound", handler);
        public void OnQueueStatus(Action<object> handler) => GetHub("match")?.On("OnQueueStatus", handler);
        public void OnServerReady(Action<object> handler) => GetHub("match")?.On("OnServerReady", handler);
        public void OnMatchAccepted(Action<object> handler) => GetHub("match")?.On("OnMatchAccepted", handler);
        public void OnRejected(Action<object> handler) => GetHub("match")?.On("OnMatchRejected", handler);

        // ==================== Duel Hub Events ====================
        public async Task JoinDuelQueue()
        {
            if (IsHubConnected("match"))
                await GetHub("match")!.InvokeAsync("JoinDuelQueue");
        }
        public async Task LeaveDuelQueue()
        {
            if (IsHubConnected("match"))
                await GetHub("match")!.InvokeAsync("LeaveDuelQueue");
        }
        public void OnDuelMatchFound(Action<object> handler) => GetHub("match")?.On("OnDuelMatchFound", handler);
        public void OnDuelServerReady(Action<object> handler) => GetHub("match")?.On("OnDuelServerReady", handler);
        public void OnDuelInviteReceived(Action<object> handler) => GetHub("chat")?.On("OnDuelInviteReceived", handler);
        public async Task CancelDuelMatch(string matchId, long opponentUserId)
        {
            if (IsHubConnected("match"))
                await GetHub("match")!.InvokeAsync("CancelDuelMatch", matchId, opponentUserId);
        }
        public void OnDuelCancelled(Action<object> handler) => GetHub("match")?.On("OnDuelCancelled", handler);

        // ==================== Chat Hub ====================
        public async Task SendPrivateMessage(long toUserId, string content)
        {
            if (IsHubConnected("chat"))
                await GetHub("chat")!.InvokeAsync("SendPrivateMessage", toUserId, content);
        }
        public async Task SendRoomMessage(string roomCode, string content)
        {
            if (IsHubConnected("chat"))
                await GetHub("chat")!.InvokeAsync("SendRoomMessage", roomCode, content);
        }
        public void OnPrivateMessage(Action<object> handler) => GetHub("chat")?.On("OnPrivateMessage", handler);
        public void OnRoomMessage(Action<object> handler) => GetHub("chat")?.On("OnRoomMessage", handler);

        // ==================== Game Hub ====================
        public async Task JoinRoom(string roomCode)
        {
            if (IsHubConnected("game"))
                await GetHub("game")!.InvokeAsync("JoinRoom", roomCode);
        }
        public async Task LeaveRoom(string roomCode)
        {
            if (IsHubConnected("game"))
                await GetHub("game")!.InvokeAsync("LeaveRoom", roomCode);
        }
        public async Task GameStarted(string roomCode)
        {
            if (IsHubConnected("game"))
                await GetHub("game")!.InvokeAsync("GameStarted", roomCode);
        }
        public async Task UpdateScore(string roomCode, int ctScore, int tScore)
        {
            if (IsHubConnected("game"))
                await GetHub("game")!.InvokeAsync("UpdateScore", roomCode, ctScore, tScore);
        }
        public void OnGameStarted(Action<object> handler) => GetHub("game")?.On("OnGameStarted", handler);
        public void OnScoreUpdate(Action<object> handler) => GetHub("game")?.On("OnScoreUpdate", handler);

        // ==================== Voice/Call Hub (通用信令) ====================
        public async Task SendVoiceSignal(string type, string data)
        {
            if (IsHubConnected("chat"))
                await GetHub("chat")!.InvokeAsync("SendVoiceSignal", type, data);
        }
        public void OnVoiceSignal(Action<string, string> handler)
        {
            GetHub("chat")?.On<string, string>("OnVoiceSignal", (type, data) =>
            {
                _dispatcher.Invoke(() => handler(type, data));
            });
        }
        public async Task JoinChannelAsync(string channelId)
        {
            if (IsHubConnected("chat"))
                await GetHub("chat")!.InvokeAsync("JoinChannel", channelId);
        }
        public async Task LeaveChannelAsync(string channelId)
        {
            if (IsHubConnected("chat"))
                await GetHub("chat")!.InvokeAsync("LeaveChannel", channelId);
        }

        // ==================== Screen Share ====================
        public async Task StartScreenShare(string channelId)
        {
            if (IsHubConnected("game"))
                await GetHub("game")!.InvokeAsync("StartScreenShare", channelId);
        }
        public async Task StopScreenShare(string channelId)
        {
            if (IsHubConnected("game"))
                await GetHub("game")!.InvokeAsync("StopScreenShare", channelId);
        }
        // 广播接收
        public event Action<object>? OnServerBroadcast;
        public void ListenBroadcast()
        {
            var hub = GetHub("broadcast");
            if (hub != null)
            {
                hub.On("OnServerBroadcast", (object data) =>
                {
                    _dispatcher.Invoke(() => OnServerBroadcast?.Invoke(data));
                });
            }
        }
    }
}
