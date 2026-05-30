using System;
using System.Windows;
using System.Windows.Media;

namespace BeiShuiCS2
{
    public static class ThemeSwitcher
    {
        private static bool _isDarkTheme = true;
        public static bool IsDarkTheme => _isDarkTheme;

        // 主题变更事件，用于通知 GlowEffectHelper 更新所有发光效果
        public static event EventHandler<ThemeChangedEventArgs> ThemeChanged = null!;

        public static void ApplyTheme(bool isDark)
        {
            _isDarkTheme = isDark;
            var primaryColor = isDark ? ColorFromHex("#4ADE80") : ColorFromHex("#16A34A");
            ApplyColors();
            ApplyBackgroundToAllWindows();
            // 触发事件通知所有发光效果更新
            ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(primaryColor, _isDarkTheme));
        }

        public static void ToggleTheme()
        {
            ApplyTheme(!_isDarkTheme);
        }

        private static void ApplyColors()
        {
            var app = Application.Current;
            if (app == null) return;

            if (_isDarkTheme)
            {
                // 暗色主题 - 极光绿
                SetColor("BgDeepColor", "#0A0F0A");
                SetColor("BgCardColor", "#141F17");
                SetColor("BgElevatedColor", "#1A2E1F");
                SetColor("BgHoverColor", "#243828");
                SetColor("BgGradientEnd", "#0D1510");
                SetColor("BorderPrimaryColor", "#2D4A35");
                SetColor("TextPrimaryColor", "#E8F5E9");
                SetColor("TextSecondaryColor", "#94A3B8");
                SetColor("TextMutedColor", "#64748B");
                // 暗色主题的主色调
                SetColor("PrimaryColor", "#4ADE80");
                SetColor("PrimaryGlowColor", "#22C55E");
            }
            else
            {
                // 亮色主题 - 清新白
                SetColor("BgDeepColor", "#F0F4F0");
                SetColor("BgCardColor", "#FFFFFF");
                SetColor("BgElevatedColor", "#F8FAF8");
                SetColor("BgHoverColor", "#E8F0E8");
                SetColor("BgGradientEnd", "#E5EBE5");
                SetColor("BorderPrimaryColor", "#D0E0D0");
                SetColor("TextPrimaryColor", "#1A2E1A");
                SetColor("TextSecondaryColor", "#5A6A5A");
                SetColor("TextMutedColor", "#8A9A8A");
                // 亮色主题的主色调（稍暗一些以便在亮色背景上可见）
                SetColor("PrimaryColor", "#16A34A");
                SetColor("PrimaryGlowColor", "#22C55E");
            }
        }

        private static void ApplyBackgroundToAllWindows()
        {
            var app = Application.Current;
            if (app == null) return;

            foreach (Window win in app.Windows)
            {
                try
                {
                    win.Background = GetBrush("BgDeepBrush");
                }
                catch { }
            }
        }

        private static void SetColor(string key, string hex)
        {
            var app = Application.Current;
            if (app == null) return;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);

                if (app.Resources.Contains(key))
                {
                    app.Resources[key] = color;
                }

                // 同时更新对应的Brush
                var brushKey = key.Replace("Color", "Brush");
                if (app.Resources.Contains(brushKey))
                {
                    app.Resources[brushKey] = new SolidColorBrush(color);
                }
            }
            catch { }
        }

        private static Brush GetBrush(string key)
        {
            var app = Application.Current;
            if (app == null) return Brushes.Transparent;

            try
            {
                if (app.Resources.Contains(key))
                {
                    return (Brush)app.Resources[key];
                }
            }
            catch { }

            return Brushes.Transparent;
        }

        private static Color ColorFromHex(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Color.FromRgb(74, 222, 128); // #4ADE80 fallback
            }
        }
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public Color PrimaryColor { get; }
        public bool IsDarkTheme { get; }

        public ThemeChangedEventArgs(Color primaryColor, bool isDarkTheme)
        {
            PrimaryColor = primaryColor;
            IsDarkTheme = isDarkTheme;
        }
    }
}
