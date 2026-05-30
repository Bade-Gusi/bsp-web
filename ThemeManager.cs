using System;
using System.Windows;
using System.Windows.Media;

namespace BeiShuiCS2
{
    public static class ThemeManager
    {
        public static bool IsDarkTheme { get; private set; } = true;

        public static void ApplyTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            ApplyColors();
        }

        public static void ToggleTheme()
        {
            ApplyTheme(!IsDarkTheme);
        }

        private static void ApplyColors()
        {
            var app = Application.Current;
            if (app == null) return;

            // 暗色主题 (Aurora Bright - 提亮版极光绿)
            if (IsDarkTheme)
            {
                SetColor("PrimaryColor",       "#4ADE80");
                SetColor("PrimaryGlowColor",   "#22C55E");
                SetColor("AccentColor",        "#4ADE80");
                SetColor("AccentTealColor",    "#2DD4BF");
                SetColor("AccentPurpleColor",  "#A78BFA");
                SetColor("DangerColor",        "#FB7185");
                SetColor("WarningColor",       "#FBBF24");
                SetColor("InfoColor",          "#60A5FA");

                SetColor("BgDeepColor",        "#0A1510");
                SetColor("BgCardColor",        "#112418");
                SetColor("BgElevatedColor",    "#1B3424");
                SetColor("BgHoverColor",       "#264432");
                SetColor("BgOverlayColor",     "#0A0F0C");

                SetColor("TextPrimaryColor",   "#F0F4F1");
                SetColor("TextSecondaryColor", "#AEC0B4");
                SetColor("TextMutedColor",     "#788A80");

                SetColor("BorderPrimaryColor", "#2D503B");
                SetColor("BorderFocusedColor", "#4ADE80");
            }
            // 亮色主题
            else
            {
                SetColor("PrimaryColor",       "#16A34A");
                SetColor("PrimaryGlowColor",   "#22C55E");
                SetColor("AccentColor",        "#16A34A");
                SetColor("AccentTealColor",    "#0D9488");
                SetColor("AccentPurpleColor",  "#7C3AED");
                SetColor("DangerColor",        "#E11D48");
                SetColor("WarningColor",       "#D97706");
                SetColor("InfoColor",          "#2563EB");

                SetColor("BgDeepColor",        "#F0F2F0");
                SetColor("BgCardColor",        "#FFFFFF");
                SetColor("BgElevatedColor",    "#FAFAFA");
                SetColor("BgHoverColor",       "#E2E8E2");
                SetColor("BgOverlayColor",     "#000000");

                SetColor("TextPrimaryColor",   "#0F1A12");
                SetColor("TextSecondaryColor", "#5A6B62");
                SetColor("TextMutedColor",     "#94A39B");

                SetColor("BorderPrimaryColor", "#D0D8D3");
                SetColor("BorderFocusedColor", "#16A34A");
            }

            // 更新窗口背景
            foreach (Window win in app.Windows)
            {
                win.Background = GetBrush("BgDeepBrush");
            }
        }

        private static void SetColor(string key, string hex)
        {
            var app = Application.Current;
            if (app == null) return;

            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);

            if (app.Resources.Contains(key))
                app.Resources[key] = color;

            var brushKey = key.Replace("Color", "Brush");
            if (app.Resources.Contains(brushKey))
                app.Resources[brushKey] = brush;
        }

        private static Brush GetBrush(string key)
        {
            var app = Application.Current;
            if (app == null) return Brushes.Transparent;

            if (app.Resources.Contains(key))
                return (Brush)app.Resources[key];

            return Brushes.Transparent;
        }
    }
}
