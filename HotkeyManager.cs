using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace BeiShuiCS2
{
    /// <summary>
    /// 全局快捷键管理器（使用 Win32 RegisterHotKey）
    /// 修复：防重复注册 + DispatcherTimer 生命周期管理
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private readonly Dictionary<int, Action> _hotkeys = new();
        private readonly Dictionary<(ModifierKeys, Key), int> _comboToId = new();
        private readonly Window _window;
        private IntPtr _handle;
        private HwndSource? _source;
        private int _nextId = 1;
        private bool _initialized;
        private bool _disposed;

        public HotkeyManager(Window window)
        {
            _window = window;
        }

        public void Initialize()
        {
            if (_initialized) return;
            _handle = new WindowInteropHelper(_window).Handle;
            _source = HwndSource.FromHwnd(_handle);
            _source?.AddHook(WndProc);
            _initialized = true;
        }

        public bool Register(ModifierKeys modifiers, Key key, Action callback)
        {
            // 防重复：相同组合键不能注册两次
            var combo = (modifiers, key);
            if (_comboToId.ContainsKey(combo))
                return false;

            int id = _nextId++;
            uint fsModifiers = 0;
            if ((modifiers & ModifierKeys.Alt) != 0) fsModifiers |= MOD_ALT;
            if ((modifiers & ModifierKeys.Control) != 0) fsModifiers |= MOD_CONTROL;
            if ((modifiers & ModifierKeys.Shift) != 0) fsModifiers |= MOD_SHIFT;
            if ((modifiers & ModifierKeys.Windows) != 0) fsModifiers |= MOD_WIN;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

            if (RegisterHotKey(_handle, id, fsModifiers, vk))
            {
                _hotkeys[id] = callback;
                _comboToId[combo] = id;
                return true;
            }
            return false;
        }

        public bool Unregister(ModifierKeys modifiers, Key key)
        {
            var combo = (modifiers, key);
            if (!_comboToId.TryGetValue(combo, out var id))
                return false;

            UnregisterHotKey(_handle, id);
            _hotkeys.Remove(id);
            _comboToId.Remove(combo);
            return true;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeys.TryGetValue(id, out var callback))
                {
                    callback();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _source?.RemoveHook(WndProc);
            foreach (int id in _hotkeys.Keys)
            {
                UnregisterHotKey(_handle, id);
            }
            _hotkeys.Clear();
            _comboToId.Clear();
            _disposed = true;
        }

        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
