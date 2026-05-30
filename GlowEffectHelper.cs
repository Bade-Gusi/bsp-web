using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BeiShuiCS2
{
    /// <summary>
    /// 动态发光效果助手
    /// 通过 AttachedProperty 实现运行时颜色更新（解决DropShadowEffect.Color不支持Binding的问题）
    /// 使用方法：在XAML中添加 xmlns:local="clr-namespace:BeiShuiCS2" 然后 e:GlowEffectHelper.ApplyGlowEffect="True"
    /// </summary>
    public static class GlowEffectHelper
    {
        // 全局发光效果注册表
        private static readonly System.Collections.Generic.List<WeakReference<UIElement>> _glowingElements =
            new System.Collections.Generic.List<WeakReference<UIElement>>();

        static GlowEffectHelper()
        {
            // 订阅主题变更事件
            ThemeSwitcher.ThemeChanged += OnThemeChanged;
        }

        private static void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            UpdateAllGlowEffects(e.PrimaryColor);
        }

        private static void UpdateAllGlowEffects(Color color)
        {
            // 清理失效的引用并更新颜色
            _glowingElements.RemoveAll(wr =>
            {
                if (!wr.TryGetTarget(out var element)) return true;
                if (element.Effect is DropShadowEffect shadow)
                {
                    shadow.Color = color;
                }
                return false;
            });
        }

        #region ApplyGlowEffect Attached Property (一键应用)

        /// <summary>
        /// 一键应用发光效果，自动跟随主题主色变化
        /// </summary>
        public static readonly DependencyProperty ApplyGlowEffectProperty =
            DependencyProperty.RegisterAttached(
                "ApplyGlowEffect",
                typeof(bool),
                typeof(GlowEffectHelper),
                new PropertyMetadata(false, OnApplyGlowEffectChanged));

        public static bool GetApplyGlowEffect(DependencyObject obj)
        {
            return (bool)obj.GetValue(ApplyGlowEffectProperty);
        }

        public static void SetApplyGlowEffect(DependencyObject obj, bool value)
        {
            obj.SetValue(ApplyGlowEffectProperty, value);
        }

        private static void OnApplyGlowEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue) return;
            if (!(d is UIElement element)) return;

            // 获取当前主题主色
            var app = Application.Current;
            Color themeColor = ColorFromHex("#4ADE80");

            if (app?.Resources.Contains("PrimaryColor") == true)
            {
                try
                {
                    themeColor = (Color)app.Resources["PrimaryColor"];
                }
                catch { }
            }

            // 创建发光效果
            var shadow = new DropShadowEffect
            {
                Color = themeColor,
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.4
            };

            element.Effect = shadow;

            // 注册到全局列表以便主题切换时更新
            _glowingElements.Add(new WeakReference<UIElement>(element));
        }

        #endregion

        #region GlowColor Attached Property

        public static readonly DependencyProperty GlowColorProperty =
            DependencyProperty.RegisterAttached(
                "GlowColor",
                typeof(Color),
                typeof(GlowEffectHelper),
                new PropertyMetadata(ColorFromHex("#4ADE80"), OnGlowColorChanged));

        public static Color GetGlowColor(DependencyObject obj)
        {
            return (Color)obj.GetValue(GlowColorProperty);
        }

        public static void SetGlowColor(DependencyObject obj, Color value)
        {
            obj.SetValue(GlowColorProperty, value);
        }

        private static void OnGlowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                UpdateEffectColor(element, (Color)e.NewValue);
            }
        }

        #endregion

        #region GlowIntensity Attached Property

        public static readonly DependencyProperty GlowIntensityProperty =
            DependencyProperty.RegisterAttached(
                "GlowIntensity",
                typeof(double),
                typeof(GlowEffectHelper),
                new PropertyMetadata(0.4, OnGlowIntensityChanged));

        public static double GetGlowIntensity(DependencyObject obj)
        {
            return (double)obj.GetValue(GlowIntensityProperty);
        }

        public static void SetGlowIntensity(DependencyObject obj, double value)
        {
            obj.SetValue(GlowIntensityProperty, value);
        }

        private static void OnGlowIntensityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                UpdateEffectIntensity(element, (double)e.NewValue);
            }
        }

        #endregion

        #region GlowRadius Attached Property

        public static readonly DependencyProperty GlowRadiusProperty =
            DependencyProperty.RegisterAttached(
                "GlowRadius",
                typeof(double),
                typeof(GlowEffectHelper),
                new PropertyMetadata(20.0, OnGlowRadiusChanged));

        public static double GetGlowRadius(DependencyObject obj)
        {
            return (double)obj.GetValue(GlowRadiusProperty);
        }

        public static void SetGlowRadius(DependencyObject obj, double value)
        {
            obj.SetValue(GlowRadiusProperty, value);
        }

        private static void OnGlowRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                UpdateEffectRadius(element, (double)e.NewValue);
            }
        }

        #endregion

        #region Helper Methods

        private static void UpdateEffectColor(UIElement element, Color color)
        {
            if (element.Effect is DropShadowEffect shadow)
            {
                shadow.Color = color;
            }
        }

        private static void UpdateEffectIntensity(UIElement element, double intensity)
        {
            if (element.Effect is DropShadowEffect shadow)
            {
                shadow.Opacity = Math.Clamp(intensity, 0.0, 1.0);
            }
        }

        private static void UpdateEffectRadius(UIElement element, double radius)
        {
            if (element.Effect is DropShadowEffect shadow)
            {
                shadow.BlurRadius = radius;
            }
        }

        #endregion

        private static Color ColorFromHex(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Color.FromRgb(74, 222, 128);
            }
        }
    }
}
