using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BeiShuiCS2
{
    public enum ViolationType
    {
        None, Debugger, BlacklistedProcess, BlacklistedWindow,
        Injection, Macro, PacketSniffer, Tamper, Integrity, AntiCheatSelf
    }

    public class CheckResult
    {
        public bool Ok { get; set; }
        public string Reason { get; set; } = "";
        public ViolationType Type { get; set; } = ViolationType.None;
    }

    public static class AntiCheat
    {
        public const string Version = "BAC3.4.0 (Zero-Client)";

        // ════════════════════════════════════════════════════════════
        // VAC 安全设计（v3.4）：
        // 1. 不调用任何 user32.dll → 无 EnumWindows
        // 2. 不 OpenProcess → 不碰游戏内存/句柄
        // 3. 仅使用 .NET 托管 API + kernel32 标准调试检测
        // 4. 全部判定逻辑移到服务端 BAC 插件
        // 5. 客户端只负责数据采集和心跳上报
        // ════════════════════════════════════════════════════════════

        // ==================== P/Invoke（仅 kernel32，VAC 安全） ===
        [DllImport("kernel32.dll")]
        private static extern bool IsDebuggerPresent();

        // ==================== 黑名单进程名 ====================
        private static readonly HashSet<string> BlacklistedProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "cheatengine", "extremeinjector", "cs2injector",
            "processhacker", "autohotkey", "macrorecorder", "tinytask"
        };

        // ==================== 状态 ====================
        private static bool _antitamperInitialized;

        public static void InitAntitamper()
        {
            if (_antitamperInitialized) return;
            _antitamperInitialized = true;
            try
            {
                var methods = typeof(AntiCheat).GetMethods(BindingFlags.Static | BindingFlags.Public);
                if (!methods.Any(m => m.Name == nameof(PerformFullCheckWithReason)))
                    throw new InvalidOperationException("反作弊代码完整性校验失败");
            }
            catch { }
        }

        /// <summary>
        /// 进程名检测（托管 API，不 OpenProcess）
        /// </summary>
        private static CheckResult CheckBlacklistedProcesses()
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (BlacklistedProcesses.Contains(proc.ProcessName))
                    {
                        string reason = $"检测到可疑进程: {proc.ProcessName}";
                        System.Diagnostics.Debug.WriteLine($"[AntiCheat] {reason}");
                        return new CheckResult { Ok = false, Reason = reason, Type = ViolationType.BlacklistedProcess };
                    }
                }
                catch { }
            }
            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 窗口标题检测（托管 API Process.MainWindowTitle，不调用 user32）
        /// </summary>
        private static CheckResult CheckBlacklistedWindows()
        {
            string[] blacklistedTitles =
            {
                "Cheat Engine", "CS2 Cheat", "Aimware", "Injector",
                "AutoHotkey", "Macro Recorder", "TinyTask"
            };

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    string title = proc.MainWindowTitle;
                    if (string.IsNullOrEmpty(title)) continue;
                    foreach (var bt in blacklistedTitles)
                    {
                        if (title.Contains(bt, StringComparison.OrdinalIgnoreCase))
                        {
                            string reason = $"检测到可疑窗口: {title}";
                            System.Diagnostics.Debug.WriteLine($"[AntiCheat] {reason}");
                            return new CheckResult { Ok = false, Reason = reason, Type = ViolationType.BlacklistedWindow };
                        }
                    }
                }
                catch { }
            }
            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 调试器检测（kernel32 标准 API，VAC 安全）
        /// </summary>
        private static CheckResult CheckAntiDebug()
        {
            try
            {
                if (IsDebuggerPresent())
                    return new CheckResult { Ok = false, Reason = "检测到调试器", Type = ViolationType.Debugger };
                if (Debugger.IsAttached)
                    return new CheckResult { Ok = false, Reason = "检测到托管调试器", Type = ViolationType.Debugger };
            }
            catch { }
            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 程序集完整性检查（反射，不进进程操作）
        /// </summary>
        private static CheckResult CheckAssemblyIntegrity()
        {
            try
            {
                var type = typeof(AntiCheat);
                var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (!methods.Any(m => m.Name == nameof(PerformFullCheckWithReason)))
                    return new CheckResult { Ok = false, Reason = "反作弊代码完整性异常", Type = ViolationType.Integrity };
            }
            catch
            {
                return new CheckResult { Ok = false, Reason = "无法反射反作弊类型", Type = ViolationType.Integrity };
            }
            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 综合检查 — 所有检测均不涉及 OpenProcess/user32
        /// </summary>
        public static CheckResult PerformFullCheckWithReason()
        {
            var integrity = CheckAssemblyIntegrity();
            if (!integrity.Ok) return integrity;

            var antiDebug = CheckAntiDebug();
            if (!antiDebug.Ok) return antiDebug;

            var procCheck = CheckBlacklistedProcesses();
            if (!procCheck.Ok) return procCheck;

            var winCheck = CheckBlacklistedWindows();
            if (!winCheck.Ok) return winCheck;

            return new CheckResult { Ok = true };
        }

        public static bool PerformQuickCheck() => PerformFullCheckWithReason().Ok;
    }
}
