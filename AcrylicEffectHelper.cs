using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace BeiShuiCS2
{
    /// <summary>
    /// Windows 10/11 Acrylic 毛玻璃效果辅助类
    /// 使用 DWM API 实现系统级毛玻璃背景
    /// </summary>
    public static class AcrylicEffectHelper
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        /// <summary>
        /// 启用 Acrylic 毛玻璃效果
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="gradientColor">渐变颜色 (ARGB格式，默认暗色半透明)</param>
        public static void EnableAcrylic(Window window, uint gradientColor = 0xCC0F1A12)
        {
            if (window.AllowsTransparency)
            {
                // AllowsTransparency=True 的分层窗口不支持 DWM Acrylic
                // 使用半透明玻璃背景替代
                window.Loaded += (s, e) => ApplySimulatedGlass(window);
                return;
            }

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                window.Loaded += (s, e) => ApplyAcrylicInternal(new WindowInteropHelper(window).Handle, gradientColor);
                return;
            }
            ApplyAcrylicInternal(hwnd, gradientColor);
        }

        /// <summary>
        /// 模拟玻璃效果：窗口背景用半透明渐变毛玻璃
        /// 适用于 AllowsTransparency=True 的 WPF 窗口
        /// </summary>
        private static void ApplySimulatedGlass(Window window)
        {
            if (window.Content is Panel rootPanel)
            {
                var glassOverlay = new Border
                {
                    IsHitTestVisible = false,
                    Opacity = 0.12,
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops =
                        {
                            new GradientStop(Color.FromRgb(0x4A, 0xDE, 0x80), 0.0),
                            new GradientStop(Color.FromRgb(0x2D, 0xD4, 0xBF), 0.5),
                            new GradientStop(Color.FromRgb(0xA7, 0x8B, 0xFA), 1.0)
                        }
                    },
                    CornerRadius = new CornerRadius(16)
                };
                Panel.SetZIndex(glassOverlay, -1);
                rootPanel.Children.Insert(0, glassOverlay);
            }
        }

        private static void ApplyAcrylicInternal(IntPtr hwnd, uint gradientColor)
        {
            if (hwnd == IntPtr.Zero) return;

            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2,
                GradientColor = unchecked((int)gradientColor)
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        /// <summary>
        /// 启用模糊背景效果 (兼容 Windows 10 早期版本)
        /// </summary>
        public static void EnableBlur(Window window, uint gradientColor = 0x990F1A12)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                window.Loaded += (s, e) => ApplyBlurInternal(new WindowInteropHelper(window).Handle, gradientColor);
                return;
            }
            ApplyBlurInternal(hwnd, gradientColor);
        }

        private static void ApplyBlurInternal(IntPtr hwnd, uint gradientColor)
        {
            if (hwnd == IntPtr.Zero) return;

            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                AccentFlags = 2,
                GradientColor = unchecked((int)gradientColor)
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        /// <summary>
        /// 禁用毛玻璃效果
        /// </summary>
        public static void DisableEffect(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_DISABLED,
                AccentFlags = 0,
                GradientColor = 0
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
    }
}
