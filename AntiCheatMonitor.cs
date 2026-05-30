using System;
using System.Diagnostics;
using System.Threading;

namespace BeiShuiCS2
{
    /// <summary>
    /// 全局反作弊监控器 — 简化版（不弹窗打扰玩家）
    /// 反作弊检查全权交给服务端 BAC 插件通过 UDP 心跳完成
    /// 客户端只做后台静默检查，发现问题仅记录日志不弹窗
    /// </summary>
    public static class AntiCheatMonitor
    {
        private static Timer? _checkTimer;
        private static bool _isMonitoring = false;

        /// <summary>
        /// 启动后台静默检查
        /// </summary>
        public static void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;

            // 每 120 秒执行一次后台检查（不影响玩家）
            _checkTimer = new Timer(CheckCallback, null,
                TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(120));

            Debug.WriteLine("[AntiCheat] 后台监控已启动（静默模式）");
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public static void StopMonitoring()
        {
            _isMonitoring = false;
            _checkTimer?.Dispose();
            _checkTimer = null;
            Debug.WriteLine("[AntiCheat] 监控已停止");
        }

        /// <summary>
        /// 后台检查回调 — 仅记录日志，不弹窗
        /// </summary>
        private static void CheckCallback(object? state)
        {
            if (!_isMonitoring) return;

            try
            {
                // 仅当游戏运行时检查
                if (!IsGameRunning())
                {
                    StopMonitoring();
                    return;
                }

                // 静默执行检查，不弹窗，不中断游戏
                var result = AntiCheat.PerformFullCheckWithReason();
                if (!result.Ok)
                {
                    Debug.WriteLine($"[AntiCheat] 检测到问题（仅记录）: {result.Reason}");
                    // 不弹窗，只记录到日志
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AntiCheat] 检查异常: {ex.Message}");
            }
        }

        private static bool IsGameRunning()
        {
            try
            {
                return Process.GetProcessesByName("cs2").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMonitoring => _isMonitoring;
    }
}
