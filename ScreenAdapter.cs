using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace BeiShuiCS2
{
    /// <summary>
    /// 屏幕自适应适配器
    /// 自动适配所有分辨率，确保窗口在不同 DPI/分辨率下显示效果一致
    /// </summary>
    public static class ScreenAdapter
    {
        /// <summary>
        /// 设计基准分辨率（1920x1080 下设计）
        /// </summary>
        public const double DesignWidth = 1920;
        public const double DesignHeight = 1080;

        /// <summary>
        /// 获取当前窗口的 DPI 缩放比例
        /// </summary>
        public static double GetDpiScale(Window window)
        {
            try
            {
                var source = PresentationSource.FromVisual(window);
                if (source?.CompositionTarget != null)
                    return source.CompositionTarget.TransformToDevice.M11;
            }
            catch { }
            return 1.0;
        }

        /// <summary>
        /// 适配窗口：确保在任何分辨率下内容比例一致
        /// </summary>
        public static void AdaptWindow(Window window)
        {
            if (window == null) return;

            // 获取屏幕工作区
            var workArea = SystemParameters.WorkArea;
            double dpiScale = GetDpiScale(window);
            if (dpiScale <= 0) dpiScale = 1.0;

            // 物理像素下的可用空间
            double availWidth = workArea.Width;
            double availHeight = workArea.Height;

            // 窗口设计尺寸
            double designW = window.Width > 0 ? window.Width : 800;
            double designH = window.Height > 0 ? window.Height : 600;

            // 如果窗口大于可用空间，整体缩放
            if (designW > availWidth || designH > availHeight)
            {
                double scaleX = (availWidth - 32) / designW;
                double scaleY = (availHeight - 32) / designH;
                double scale = Math.Min(scaleX, scaleY);

                // 对整个窗口内容应用 LayoutTransform 缩放
                var transform = new ScaleTransform(scale, scale);
                window.LayoutTransform = transform;

                // 调整窗口大小以容纳缩放后的内容
                window.Width = designW;
                window.Height = designH;
            }

            // 确保在屏幕内
            ClampWindowPosition(window, dpiScale);
        }

        /// <summary>
        /// 计算适应屏幕的缩放比例（供 ViewBox 使用）
        /// </summary>
        public static double GetAdaptiveScale(Window window)
        {
            var workArea = SystemParameters.WorkArea;
            double designW = window?.Width > 0 ? window.Width : 800;
            double designH = window?.Height > 0 ? window.Height : 600;

            double scaleX = (workArea.Width - 32) / designW;
            double scaleY = (workArea.Height - 32) / designH;

            return Math.Min(1.0, Math.Min(scaleX, scaleY));
        }

        private static void ClampWindowPosition(Window window, double dpiScale)
        {
            var workArea = SystemParameters.WorkArea;

            double actualW = window.ActualWidth;
            double actualH = window.ActualHeight;

            double left = window.Left;
            double top = window.Top;

            if (double.IsNaN(left))
                left = workArea.Left + (workArea.Width - actualW) / 2;
            if (double.IsNaN(top))
                top = workArea.Top + (workArea.Height - actualH) / 2;

            if (left < workArea.Left) left = workArea.Left;
            if (top < workArea.Top) top = workArea.Top;
            if (left + actualW > workArea.Right) left = workArea.Right - actualW;
            if (top + actualH > workArea.Bottom) top = workArea.Bottom - actualH;

            window.Left = left;
            window.Top = top;
        }
    }
}
