using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace BeiShuiCS2.Services
{
    public class GameLauncherService : IDisposable
    {
        private Process? _gameProcess;
        private Timer? _monitorTimer;

        public enum GameStatus { Idle, Launching, Running, Exited, Crashed }
        public GameStatus CurrentStatus { get; private set; } = GameStatus.Idle;
        public event Action<GameStatus>? OnGameStatusChanged;

        private static readonly (string Name, string ProcessName, string SteamId)[] SupportedGames =
        {
            ("Counter-Strike 2", "cs2.exe", "730"),
            ("PUBG: BATTLEGROUNDS", "TslGame.exe", "578080"),
            ("VALORANT", "VALORANT-Win64-Shipping.exe", ""),
            ("Apex Legends", "r5apex.exe", "1172470"),
        };

        /// <summary>
        /// 查找游戏安装路径（文档方案：多策略查找）
        /// </summary>
        public string? FindGameInstallPath(string processName)
        {
            // 策略1: 已保存的CS2路径
            if (processName.Equals("cs2.exe", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(App.CS2Path))
                return Path.GetDirectoryName(App.CS2Path);

            // 策略2: Steam 库
            var steamPath = FindSteamGamePath(processName);
            if (steamPath != null) return steamPath;

            // 策略3: 注册表
            var regPath = FindGameFromRegistry(processName);
            if (regPath != null) return regPath;

            // 策略4: 常见安装目录
            var commonDirs = new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"C:\Program Files (x86)\Steam\steamapps\common",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Steam\steamapps\common"),
            };

            foreach (var dir in commonDirs)
            {
                if (!Directory.Exists(dir)) continue;
                try
                {
                    var files = Directory.GetFiles(dir, processName, SearchOption.AllDirectories);
                    if (files.Length > 0)
                        return Path.GetDirectoryName(files[0]);
                }
                catch { }
            }

            return null;
        }

        private static string? FindSteamGamePath(string processName)
        {
            try
            {
                var steamPath = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath")?.ToString();
                if (string.IsNullOrEmpty(steamPath)) return null;

                // 读取 steam 库配置
                var configPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(configPath)) return null;

                var lines = File.ReadAllLines(configPath);
                foreach (var line in lines)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"""\d+""\s+""(.+?)""");
                    if (match.Success)
                    {
                        var libPath = match.Groups[1].Value.Replace(@"\\", @"\");
                        var commonPath = Path.Combine(libPath, "steamapps", "common");
                        if (Directory.Exists(commonPath))
                        {
                            var files = Directory.GetFiles(commonPath, processName, SearchOption.AllDirectories);
                            if (files.Length > 0)
                                return Path.GetDirectoryName(files[0]);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static string? FindGameFromRegistry(string processName)
        {
            try
            {
                // 检查常见游戏的注册表路径
                var regPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                };

                foreach (var regPath in regPaths)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(regPath);
                    if (key == null) continue;
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        var installLocation = subKey?.GetValue("InstallLocation")?.ToString();
                        if (string.IsNullOrEmpty(installLocation)) continue;
                        var exePath = Path.Combine(installLocation, processName);
                        if (File.Exists(exePath))
                            return installLocation;
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 启动游戏：优先本地路径，否则走 Steam 协议
        /// </summary>
        public Task<bool> LaunchGame(string serverAddress, string? launchArgs = null)
        {
            if (App.AntiCheatBlocked) return Task.FromResult(false);

            var settings = AppSettings.Load();
            string args = string.IsNullOrEmpty(launchArgs)
                ? $"-steam +connect {serverAddress}"
                : $"{launchArgs} +connect {serverAddress}";

            // 方式一：本地 CS2 路径
            if (!string.IsNullOrEmpty(App.CS2Path) && File.Exists(App.CS2Path))
            {
                try
                {
                    string fullArgs = string.IsNullOrEmpty(settings.LaunchArgs)
                        ? args
                        : $"{settings.LaunchArgs} {args}";

                    _gameProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = App.CS2Path,
                            Arguments = fullArgs,
                            UseShellExecute = true,
                            Verb = "runas",
                            WorkingDirectory = Path.GetDirectoryName(App.CS2Path)
                        }
                    };
                    _gameProcess.Start();
                    CurrentStatus = GameStatus.Launching;
                    OnGameStatusChanged?.Invoke(GameStatus.Launching);
                    StartProcessMonitor("cs2.exe");
                    return Task.FromResult(true);
                }
                catch { }
            }

            // 方式二：Steam 协议
            try
            {
                string uri = $"steam://rungameid/730//+connect {serverAddress}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });
                CurrentStatus = GameStatus.Launching;
                OnGameStatusChanged?.Invoke(GameStatus.Launching);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 通过 Steam 协议启动任意游戏
        /// </summary>
        public Task<bool> LaunchViaSteam(string steamAppId, string? args = null)
        {
            try
            {
                var uri = string.IsNullOrEmpty(args)
                    ? $"steam://rungameid/{steamAppId}"
                    : $"steam://rungameid/{steamAppId}//{args}";
                Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private void StartProcessMonitor(string? processName = null)
        {
            _monitorTimer?.Dispose();
            _monitorTimer = new Timer(_ =>
            {
                try
                {
                    // 多游戏进程检测：支持配置多个进程名
                    bool running = IsAnyGameRunning(processName ?? "cs2");
                    if (running && CurrentStatus != GameStatus.Running)
                    {
                        CurrentStatus = GameStatus.Running;
                        OnGameStatusChanged?.Invoke(GameStatus.Running);
                    }
                    else if (!running && CurrentStatus == GameStatus.Running)
                    {
                        CurrentStatus = GameStatus.Exited;
                        OnGameStatusChanged?.Invoke(GameStatus.Exited);
                        _monitorTimer?.Dispose();
                    }
                }
                catch { }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// 检测任意游戏进程是否在运行（多游戏支持）
        /// </summary>
        public static bool IsAnyGameRunning(string? specificProcess = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(specificProcess))
                {
                    var nameOnly = Path.GetFileNameWithoutExtension(specificProcess);
                    return Process.GetProcessesByName(nameOnly).Length > 0;
                }

                // 检测所有支持的游戏
                foreach (var (_, procName, _) in SupportedGames)
                {
                    var nameOnly = Path.GetFileNameWithoutExtension(procName);
                    if (Process.GetProcessesByName(nameOnly).Length > 0)
                        return true;
                }
                return false;
            }
            catch { return false; }
        }

        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }
}
