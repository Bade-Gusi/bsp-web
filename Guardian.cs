using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace BeiShuiCS2.Guardian
{
    class Program
    {
        public static async Task RunGuardian(string[] args)  // 原 Main 改为 RunGuardian
        {
            if (args.Length < 2) return;

            if (!int.TryParse(args[0], out int parentPid))
                return;

            string serverAddress = args[1];

            Process? parent = null;
            try { parent = Process.GetProcessById(parentPid); }
            catch { return; }

            Process? game = null;
            for (int i = 0; i < 30; i++)
            {
                game = FindGameProcess(serverAddress);
                if (game != null) break;
                await Task.Delay(1000);
            }

            if (game == null)
                return;

            var tcs = new TaskCompletionSource<bool>();
            parent.EnableRaisingEvents = true;
            parent.Exited += (s, e) => tcs.TrySetResult(true);
            game.EnableRaisingEvents = true;
            game.Exited += (s, e) => tcs.TrySetResult(true);

            await tcs.Task;

            if (!game.HasExited && !IsProcessAlive(parent))
            {
                try { game.Kill(); game.WaitForExit(3000); } catch { }
            }
        }

        private static Process? FindGameProcess(string serverAddress)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='cs2.exe'");
                foreach (var obj in searcher.Get())
                {
                    int pid = Convert.ToInt32(obj["ProcessId"]);
                    string cmdLine = obj["CommandLine"]?.ToString() ?? "";
                    if (cmdLine.Contains($"+connect {serverAddress}") ||
                        cmdLine.Contains($"\"+connect {serverAddress}\""))
                    {
                        return Process.GetProcessById(pid);
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool IsProcessAlive(Process proc)
        {
            try { return !proc.HasExited; }
            catch { return false; }
        }
    }
}