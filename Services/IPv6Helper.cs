using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;

namespace BeiShuiCS2.Services
{
    public static class IPv6Helper
    {
        /// <summary>
        /// 检查本机 IPv6 是否已启用
        /// </summary>
        public static bool IsIPv6Enabled()
        {
            try
            {
                // 检查是否有 IPv6 网络接口
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    var props = ni.GetIPProperties();
                    if (props.UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6 && !a.Address.IsIPv6LinkLocal))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查 DNS 是否能解析 IPv6 地址
        /// </summary>
        public static bool CanResolveIPv6(string hostname)
        {
            try
            {
                var addrs = Dns.GetHostAddresses(hostname);
                return addrs.Any(a => a.AddressFamily == AddressFamily.InterNetworkV6);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取本机 IPv6 地址列表
        /// </summary>
        public static string[] GetLocalIPv6Addresses()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6 && !a.Address.IsIPv6LinkLocal)
                    .Select(a => a.Address.ToString())
                    .Distinct()
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 启用 IPv6（需要管理员权限）
        /// </summary>
        public static void EnableIPv6(Window? owner = null)
        {
            var result = MessageBox.Show(
                "需要启用 IPv6 才能连接到服务器。将以管理员权限运行 PowerShell 启用 IPv6。\n\n" +
                "操作完成后请重启本软件。是否继续？",
                "启用 IPv6",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            RunPowerShellCommand(
                "Enable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6",
                "启用 IPv6",
                owner);
        }

        /// <summary>
        /// 禁用 IPv6
        /// </summary>
        public static void DisableIPv6(Window? owner = null)
        {
            var result = MessageBox.Show(
                "确定要禁用 IPv6 吗？\n\n" +
                "禁用后可能无法连接某些 IPv6 优先的服务器。",
                "禁用 IPv6",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            RunPowerShellCommand(
                "Disable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6",
                "禁用 IPv6",
                owner);
        }

        /// <summary>
        /// 刷新 IPv6 状态并返回当前状态文本
        /// </summary>
        public static string GetIPv6StatusText()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-NetAdapterBinding -ComponentID ms_tcpip6 | Select-Object Name, Enabled | Format-Table -AutoSize\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return "无法检查 IPv6 状态";
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output.Trim();
            }
            catch
            {
                return "检查失败（需要管理员权限）";
            }
        }

        private static void RunPowerShellCommand(string command, string actionName, Window? owner = null)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                MessageBox.Show(
                    $"{actionName}操作已启动！\n\n" +
                    "请在弹出的 PowerShell 窗口中确认（如有 UAC 提示请点击[是]）。\n" +
                    "完成后请重启本软件。",
                    actionName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"操作失败：{ex.Message}\n\n请以管理员权限手动运行 PowerShell：\n{command}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
