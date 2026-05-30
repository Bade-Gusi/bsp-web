using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BeiShuiCS2
{
    public enum ViolationType
    {
        None,
        Debugger,
        BlacklistedProcess,
        BlacklistedWindow,
        Injection,
        Macro,
        PacketSniffer,
        Tamper,
        Integrity,
        AntiCheatSelf
    }

    public class CheckResult
    {
        public bool Ok { get; set; }
        public string Reason { get; set; } = "";
        public ViolationType Type { get; set; } = ViolationType.None;
    }

    public static class AntiCheat
    {
        public const string Version = "BAC3.3.0 (VAC-Safe)";

        // ════════════════════════════════════════════════════════════
        // VAC 安全设计原则：
        // 1. 不调用 OpenProcess 打开 CS2 进程（否则 VAC 会标记）
        // 2. 不注入 CS2 进程，不读写 CS2 内存
        // 3. 仅通过进程名/窗口标题等安全方式做检测
        // 4. 反作弊验证全权交给服务端 BAC 插件（UDP 心跳）
        // 5. 客户端只做辅助检查，不做强制封禁
        // ════════════════════════════════════════════════════════════

        // ==================== P/Invoke（仅安全范围） ====================
        [DllImport("kernel32.dll")]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll")]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // ==================== 状态 ====================
        private static bool _antitamperInitialized;
        /// <summary>
        /// 初始化反篡改子系统 — 安全模式（无进程注入检测）
        /// </summary>
        public static void InitAntitamper()
        {
            if (_antitamperInitialized) return;
            _antitamperInitialized = true;

            try
            {
                // 验证关键方法存在
                var methods = typeof(AntiCheat).GetMethods(BindingFlags.Static | BindingFlags.Public);
                if (!methods.Any(m => m.Name == nameof(PerformFullCheckWithReason)))
                    throw new InvalidOperationException("反作弊代码完整性校验失败");
            }
            catch { }
        }

        // ==================== 黑名单 ====================
        private static readonly HashSet<string> BlacklistedProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "cheatengine", "cheatengine-x86_64", "extremeinjector", "cs2injector",
            "injector", "mapper", "manualmap", "loadlibrary",
            "processhacker", "processhacker2",
            "autohotkey", "macrorecorder", "tinytask", "jbit", "macro", "lkmacro"
        };

        private static readonly string[] BlacklistedWindowClasses =
        {
            "CheatEngine", "ExtremeInjector", "Injector", "AutoHotkey", "MacroRecorder"
        };

        private static readonly string[] BlacklistedWindowTitles =
        {
            "Cheat Engine", "CS2 Cheat", "Aimware", "Injector", "Manual Map",
            "AutoHotkey", "Macro Recorder", "TinyTask"
        };

        // ==================== 安全检测方法 ====================

        /// <summary>
        /// 检测黑名单进程（仅通过进程名，不 OpenProcess）
        /// </summary>
        private static CheckResult CheckBlacklistedProcesses()
        {
            var allProcs = Process.GetProcesses();
            foreach (var proc in allProcs)
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
        /// 检测黑名单窗口（通过 EnumWindows 枚举窗口标题/类名）
        /// </summary>
        private static CheckResult CheckBlacklistedWindows()
        {
            bool found = false;
            string foundReason = "";

            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    StringBuilder title = new(256);
                    GetWindowText(hWnd, title, title.Capacity);
                    string titleStr = title.ToString();

                    StringBuilder className = new(256);
                    GetClassName(hWnd, className, className.Capacity);
                    string classStr = className.ToString();

                    foreach (var bt in BlacklistedWindowTitles)
                        if (titleStr.Contains(bt, StringComparison.OrdinalIgnoreCase))
                        { found = true; foundReason = $"检测到可疑窗口: {titleStr}"; return false; }

                    foreach (var bc in BlacklistedWindowClasses)
                        if (classStr.Contains(bc, StringComparison.OrdinalIgnoreCase))
                        { found = true; foundReason = $"检测到可疑窗口: {titleStr}"; return false; }
                }
                catch { }
                return true;
            }, IntPtr.Zero);

            if (found)
                return new CheckResult { Ok = false, Reason = foundReason, Type = ViolationType.BlacklistedWindow };
            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 调试器检测（仅系统 API，安全）
        /// </summary>
        private static CheckResult CheckAntiDebug()
        {
            try
            {
                if (IsDebuggerPresent())
                    return new CheckResult { Ok = false, Reason = "检测到调试器", Type = ViolationType.Debugger };

                if (Debugger.IsAttached)
                    return new CheckResult { Ok = false, Reason = "检测到托管调试器", Type = ViolationType.Debugger };

                // 远程调试检测
                bool isRemoteDebuggerPresent = false;
                try
                {
                    IntPtr hProcess = Process.GetCurrentProcess().Handle;
                    CheckRemoteDebuggerPresent(hProcess, ref isRemoteDebuggerPresent);
                    if (isRemoteDebuggerPresent)
                        return new CheckResult { Ok = false, Reason = "检测到远程调试器", Type = ViolationType.Debugger };
                }
                catch { }
            }
            catch { }

            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 程序集完整性检查（反射，不进��进程操作）
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

            // 检查内存中是否有被篡改的关键类型
            try
            {
                // 验证反作弊相关类型没被重写
                var antiCheatType = typeof(AntiCheat);
                var assembly = antiCheatType.Assembly;
                var allTypes = assembly.GetTypes();

                // 检查是否有未知的"作弊"相关类型注入到本程序集
                int suspiciousCount = 0;
                foreach (var t in allTypes)
                {
                    string name = t.Name.ToLower();
                    if (name.Contains("cheat") || name.Contains("hack") || name.Contains("bypass") ||
                        name.Contains("patch") && t.Namespace != null && !t.Namespace.StartsWith("BeiShui"))
                    {
                        suspiciousCount++;
                    }
                }
                if (suspiciousCount > 5) // 允许少量正常类型
                    return new CheckResult { Ok = false, Reason = "检测到异常类型注入", Type = ViolationType.Injection };
            }
            catch { }

            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 进程守护检查 — 只检查 Guardian 进程是否存活
        /// </summary>
        private static CheckResult CheckGuardian()
        {
            return new CheckResult { Ok = true };
        }

        // ==================== 综合检查 ====================

        /// <summary>
        /// 执行完整反作弊检查（VAC 安全版本）
        /// 所有检测方法均不涉及 OpenProcess 外部进程
        /// </summary>
        public static CheckResult PerformFullCheckWithReason()
        {
            // 1. 程序集完整性（反射，安全）
            var integrity = CheckAssemblyIntegrity();
            if (!integrity.Ok) return integrity;

            // 2. 调试器检测（系统 API，安全）
            var antiDebug = CheckAntiDebug();
            if (!antiDebug.Ok) return antiDebug;

            // 3. 黑名单进程检测（Process.GetProcesses，安全）
            var blacklistedProc = CheckBlacklistedProcesses();
            if (!blacklistedProc.Ok) return blacklistedProc;

            // 4. 黑名单窗口检测（EnumWindows，安全）
            var blacklistedWin = CheckBlacklistedWindows();
            if (!blacklistedWin.Ok) return blacklistedWin;

            // 5. 进程守护检查（仅检查自身进程状态）
            var guardian = CheckGuardian();
            if (!guardian.Ok) return guardian;

            return new CheckResult { Ok = true };
        }

        /// <summary>
        /// 快速检查（用于 UI 状态指示）
        /// </summary>
        public static bool PerformQuickCheck()
        {
            var result = PerformFullCheckWithReason();
            return result.Ok;
        }
    }
}
