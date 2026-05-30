using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using System.Windows.Threading;

namespace BeiShuiCS2.Services
{
    public class SignalRClient
    {
        private HubConnection? _connection;
        private readonly Dispatcher _dispatcher;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public event Action<string>? OnUserJoined;
        public event Action<string>? OnUserLeft;
        public event Action<string, string, string>? OnSignalReceived;
        public event Action<string>? OnScreenShareStarted;
        public event Action<string>? OnScreenShareStopped;
        public event Action<bool>? OnConnectionChanged;

        public SignalRClient()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public async Task ConnectAsync()
        {
            var hubUrl = NetworkHelper.BuildHubUrl();

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // 跳过自签名证书验证
                    if (hubUrl.StartsWith("https"))
                        options.HttpMessageHandlerFactory = _ => new System.Net.Http.HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                        };
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // 注册事件处理
            _connection.On<string>("UserJoined", userId =>
                _dispatcher.Invoke(() => OnUserJoined?.Invoke(userId)));
            _connection.On<string>("UserLeft", userId =>
                _dispatcher.Invoke(() => OnUserLeft?.Invoke(userId)));
            _connection.On<string, string, string>("ReceiveSignal", (fromId, type, data) =>
                _dispatcher.Invoke(() => OnSignalReceived?.Invoke(fromId, type, data)));
            _connection.On<string>("ScreenShareStarted", userId =>
                _dispatcher.Invoke(() => OnScreenShareStarted?.Invoke(userId)));
            _connection.On<string>("ScreenShareStopped", userId =>
                _dispatcher.Invoke(() => OnScreenShareStopped?.Invoke(userId)));

            _connection.Closed += async (error) =>
            {
                _dispatcher.Invoke(() => OnConnectionChanged?.Invoke(false));
                await Task.Delay(5000);
                try { await _connection.StartAsync(); } catch { }
            };

            _connection.Reconnected += async (connectionId) =>
            {
                _dispatcher.Invoke(() => OnConnectionChanged?.Invoke(true));
                await Task.CompletedTask;
            };

            try
            {
                await _connection.StartAsync();
                _dispatcher.Invoke(() => OnConnectionChanged?.Invoke(true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] 连接失败: {ex.Message}");
            }
        }

        public async Task JoinChannelAsync(string channelId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                await _connection.InvokeAsync("JoinChannel", channelId);
        }

        public async Task LeaveChannelAsync(string channelId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                await _connection.InvokeAsync("LeaveChannel", channelId);
        }

        public async Task SendSignalAsync(string channelId, string signalType, string data, string targetId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                await _connection.InvokeAsync("SendSignal", channelId, signalType, data, targetId);
        }

        public async Task StartScreenShareAsync(string channelId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                await _connection.InvokeAsync("StartScreenShare", channelId);
        }

        public async Task StopScreenShareAsync(string channelId)
        {
            if (_connection?.State == HubConnectionState.Connected)
                await _connection.InvokeAsync("StopScreenShare", channelId);
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public static string BuildHubUrl(string serverUrl)
        {
            if (Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri))
            {
                var host = uri.Host;
                if (IPAddress.TryParse(host, out var addr) && addr.AddressFamily == AddressFamily.InterNetworkV6)
                    host = $"[{host}]";
                return $"{uri.Scheme}://{host}:{uri.Port}/callhub";
            }
            return serverUrl + "/callhub";
        }
    }
}
