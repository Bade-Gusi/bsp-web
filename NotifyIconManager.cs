using System;
using System.Windows;

namespace BeiShuiCS2
{
    /// <summary>
    /// 系统托盘图标管理（最小化到托盘）
    /// 修复：菜单可随语言切换、关闭确认对话框
    /// </summary>
    public class NotifyIconManager : IDisposable
    {
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private System.Windows.Forms.ContextMenuStrip? _contextMenu;
        private readonly Window _window;
        private bool _disposed;

        public NotifyIconManager(Window window)
        {
            _window = window;
        }

        public void Create()
        {
            if (_notifyIcon != null) return;

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? ""),
                Text = "BeiShui - 背水对战平台",
                Visible = true
            };

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    ShowWindow();
            };

            // 使用当前语言创建菜单
            _contextMenu = new System.Windows.Forms.ContextMenuStrip();
            UpdateMenuLanguage();

            _notifyIcon.ContextMenuStrip = _contextMenu;

            _window.StateChanged += (s, e) =>
            {
                if (_window.WindowState == WindowState.Minimized && AppSettings.Load().StartMinimized)
                    _window.Hide();
            };
        }

        /// <summary>
        /// 更新托盘菜单文本（语言切换时调用）
        /// </summary>
        public void UpdateMenuLanguage()
        {
            if (_contextMenu == null) return;
            _contextMenu.Items.Clear();
            _contextMenu.Items.Add(GetString("显示主窗口", "Show Window"), null, (s, e) => ShowWindow());
            _contextMenu.Items.Add(GetString("快速匹配", "Quick Match"), null, (s, e) => QuickMatch());
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add(GetString("退出", "Exit"), null, (s, e) => ExitApplication());
        }

        private static string GetString(string zh, string en)
        {
            try
            {
                var settings = AppSettings.Load();
                return settings.Language == "en" ? en : zh;
            }
            catch { return zh; }
        }

        public void ShowWindow()
        {
            _window.Show();
            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;
            _window.Activate();
        }

        private void QuickMatch()
        {
            _window.Dispatcher.Invoke(() =>
            {
                if (App.AntiCheatBlocked) return;
                ShowWindow();
                var quickMatch = new QuickMatchWindow { Owner = _window };
                quickMatch.ShowDialog();
            });
        }

        private void ExitApplication()
        {
            if (_notifyIcon != null) _notifyIcon.Visible = false;
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            _notifyIcon = null;
            _contextMenu = null;
            _disposed = true;
        }
    }
}
