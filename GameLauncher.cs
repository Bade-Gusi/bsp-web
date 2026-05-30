using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BeiShuiCS2
{
    public class HeartbeatInfo
    {
        public bool IsActive { get; set; }
        public string ServerIP { get; set; } = "";
        public int Port { get; set; }
        public DateTime LastHeartbeatTime { get; set; }
        public string SteamID { get; set; } = "";
        public int PacketCount { get; set; }
        public string LastPacketContent { get; set; } = "";
        public string ClientFingerprint { get; set; } = "";
        public string AntiCheatVersion { get; set; } = "";
        public bool AntiCheatOk { get; set; }
    }

    public static class GameLauncher
    {
        private static CancellationTokenSource? heartbeatCts;
        private static Task? heartbeatTask;
        private static MainWindow? mainWindow;
        private static HeartbeatInfo currentHeartbeatInfo = new HeartbeatInfo { IsActive = false };

    public static HeartbeatInfo GetHeartbeatInfo() => currentHeartbeatInfo;

        public static bool LaunchViaSteam(string command)
        {
            if (App.AntiCheatBlocked) return false;
            try
            {
                string uri = $"steam://rungameid/730//{command}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = uri,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool LaunchDirect(string command)
        {
            if (App.AntiCheatBlocked) return false;
            if (!string.IsNullOrEmpty(App.CS2Path) && File.Exists(App.CS2Path))
            {
                try
                {
                    // 合并存储的启动参数
                    var settings = AppSettings.Load();
                    string fullArgs = string.IsNullOrEmpty(settings.LaunchArgs)
                        ? command
                        : $"{settings.LaunchArgs} {command}";

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = App.CS2Path,
                        Arguments = fullArgs,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(App.CS2Path)
                    });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        // 启动 CS2：优先用本地路径直连，否则用 steam:// 协议
        public static void LaunchGame(string serverAddress)
        {
            if (App.AntiCheatBlocked) return;
            try { System.Windows.Clipboard.SetText(serverAddress); } catch { }

            // 方式一：本地 CS2 路径直连（最可靠）
            if (!string.IsNullOrEmpty(App.CS2Path) && File.Exists(App.CS2Path))
            {
                try
                {
                    var settings = AppSettings.Load();
                    string args = string.IsNullOrEmpty(settings.LaunchArgs)
                        ? $"-steam +connect {serverAddress}"
                        : $"{settings.LaunchArgs} +connect {serverAddress}";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = App.CS2Path,
                        Arguments = args,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(App.CS2Path)
                    });
                    return;
                }
                catch { }
            }

            // 方式二：steam:// 协议兜底
            string uri = $"steam://rungameid/730//+connect {serverAddress}";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = uri,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public static void ConnectAndHeartbeat(string serverAddress)
        {
            if (mainWindow == null)
                mainWindow = Application.Current.MainWindow as MainWindow;

            LaunchGame(serverAddress);

            string serverIP = ExtractIP(serverAddress, out int gamePort);
            if (string.IsNullOrEmpty(serverIP)) return;

            mainWindow?.ShowToast($"正在连接 {serverAddress}...");
            AntiCheatMonitor.StartMonitoring();

            App.ConnectToGameServer(serverIP, gamePort);
            var steamUser = SteamService.GetCurrentSteamUser();
            if (steamUser != null)
            {
                var token = AntiCheatToken.Create(steamUser.SteamID64, App.AntiCheatPassed, AntiCheat.Version);
                StartHeartbeat(serverIP, gamePort + 1, token);
            }
            OpenAntiCheatStatusWindow();
        }

        private static void OpenAntiCheatStatusWindow()
        {
            if (mainWindow == null) return;
            mainWindow.Dispatcher.BeginInvoke((Action)(() =>
            {
                var existing = Application.Current.Windows.OfType<AntiCheatStatusWindow>().FirstOrDefault();
                if (existing != null)
                    existing.Activate();
                else
                {
                    var statusWin = new AntiCheatStatusWindow { Owner = mainWindow };
                    statusWin.Show();
                }
            }));
        }

        private static string ExtractIP(string address, out int port)
        {
            port = 27015;
            if (string.IsNullOrWhiteSpace(address)) return "";
            string[] parts = address.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int p))
            {
                port = p;
                return parts[0];
            }
            return address;
        }

        private static void StartHeartbeat(string serverIP, int heartbeatPort, AntiCheatToken token)
        {
            currentHeartbeatInfo.IsActive = true;
            currentHeartbeatInfo.ServerIP = serverIP;
            currentHeartbeatInfo.Port = heartbeatPort;
            currentHeartbeatInfo.SteamID = token.SteamID;
            currentHeartbeatInfo.PacketCount = 0;
            currentHeartbeatInfo.LastPacketContent = "";
            currentHeartbeatInfo.ClientFingerprint = AntiCheatClient.GetMachineFingerprint();
            currentHeartbeatInfo.AntiCheatVersion = AntiCheat.Version;
            currentHeartbeatInfo.AntiCheatOk = App.AntiCheatPassed && !App.AntiCheatBlocked;

            heartbeatCts = new CancellationTokenSource();
            heartbeatTask = Task.Run(async () => await HeartbeatLoop(serverIP, heartbeatPort, token, heartbeatCts.Token));
        }

        private static async Task HeartbeatLoop(string serverIP, int heartbeatPort, AntiCheatToken token, CancellationToken cancellationToken)
        {
            // 等待几秒让 AntiCheatClient 完成握手
            await Task.Delay(3000, cancellationToken);

            using var udpClient = new UdpClient();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 检查 CS2 是否还在运行（大小写不敏感）
                    var allProcs = Process.GetProcesses();
                    bool cs2Running = allProcs.Any(p => p.ProcessName.Equals("cs2", StringComparison.OrdinalIgnoreCase));
                    if (!cs2Running)
                    {
                        mainWindow?.Dispatcher.Invoke(() => mainWindow?.ShowToast("🛑 游戏已退出，停止心跳"));
                        StopHeartbeat();
                        break;
                    }

                    // 如果 AntiCheatClient 已建立加密通道，跳过旧版心跳
                    if (App.AntiCheatClient != null && App.AntiCheatClient.IsConnected)
                    {
                        await Task.Delay(15000, cancellationToken);
                        continue;
                    }

                    // 旧版 JSON 心跳（备用通道）
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string signature = ComputeLegacySignature(token.SteamID, App.AntiCheatPassed, AntiCheat.Version, now);
                    var legacyPacket = new
                    {
                        SteamID = token.SteamID,
                        Status = App.AntiCheatPassed,
                        Version = AntiCheat.Version,
                        Timestamp = now,
                        Seq = 0,
                        Signature = signature
                    };
                    currentHeartbeatInfo.AntiCheatOk = App.AntiCheatPassed && !App.AntiCheatBlocked;
                    string json = JsonSerializer.Serialize(legacyPacket);
                    byte[] dataToSend = Encoding.UTF8.GetBytes(json);
                    currentHeartbeatInfo.LastPacketContent = json;

                    await udpClient.SendAsync(dataToSend, dataToSend.Length, serverIP, heartbeatPort);
                    currentHeartbeatInfo.LastHeartbeatTime = DateTime.Now;
                    currentHeartbeatInfo.PacketCount++;

                    await Task.Delay(15000, cancellationToken);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    mainWindow?.Dispatcher.Invoke(() => mainWindow?.ShowToast($"⚠️ 心跳异常: {ex.Message}"));
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }

        private static string ComputeLegacySignature(string steamId, bool status, string version, long timestamp)
        {
            const string secret = "1M@r#9v$thfhf?346+2qL!pX7nY&3zR";
            string data = $"{steamId}|{status}|{version}|{timestamp}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        public static void StopHeartbeat()
        {
            heartbeatCts?.Cancel();
            try { heartbeatTask?.Wait(1000); } catch { }
            heartbeatCts?.Dispose();
            heartbeatCts = null;
            currentHeartbeatInfo.IsActive = false;

            // 断开 AntiCheatClient 加密通道
            App.DisconnectFromGameServer();

            mainWindow?.Dispatcher.Invoke(() => mainWindow?.ShowToast("🔌 反作弊心跳已停止"));
        }
    }
}