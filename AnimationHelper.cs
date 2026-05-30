using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    /// <summary>
    /// 全自动画与特效辅助类
    /// 注意：不要对 AllowsTransparency=True 的窗口使用 ScaleTransform，
    /// 否则会导致 WPF 渲染管线崩溃（DWM 分层窗口限制）
    /// </summary>
    public static class AnimationHelper
    {
        /// <summary>
        /// 窗口入场：透明度动画（安全版本，不操作 RenderTransform）
        /// </summary>
        public static void WindowEntrance(Window window, double delaySec = 0)
        {
            window.Opacity = 0;

            window.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        /// <summary>
        /// 窗口退出：淡出（安全版本）
        /// </summary>
        public static void WindowExit(Window window, Action? onCompleted = null)
        {
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
            fade.Completed += (_, _) => onCompleted?.Invoke();
            window.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        /// <summary>
        /// 子元素交错入场（透明度 + 上滑）
        /// </summary>
        public static void StaggerEntrance(UIElementCollection children, double itemDelay = 0.07)
        {
            int i = 0;
            foreach (UIElement child in children)
            {
                if (child is FrameworkElement fe && fe.IsVisible)
                {
                    fe.Opacity = 0;
                    var tt = new TranslateTransform(0, 15);
                    fe.RenderTransform = tt;

                    int idx = i;
                    fe.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                    {
                        BeginTime = TimeSpan.FromSeconds(itemDelay * idx),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });

                    tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(15, 0, TimeSpan.FromSeconds(0.4))
                    {
                        BeginTime = TimeSpan.FromSeconds(itemDelay * idx),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
                }
                i++;
            }
        }

        /// <summary>
        /// 安全液态玻璃效果：渐变色彩缓慢流动 + 呼吸透明度
        /// 不使用 BlurEffect（避免 AllowsTransparency 窗口崩溃）
        /// </summary>
        public static void ApplyLiquidGlass(Panel rootPanel)
        {
            var overlay = new Border
            {
                IsHitTestVisible = false,
                Opacity = 0.12
            };

            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            var s1 = new GradientStop(Color.FromRgb(0x4A, 0xDE, 0x80), 0.0);
            var s2 = new GradientStop(Color.FromRgb(0x22, 0xD3, 0xEE), 0.5);
            var s3 = new GradientStop(Color.FromRgb(0x7D, 0xD3, 0xA0), 1.0);

            brush.GradientStops.Add(s1);
            brush.GradientStops.Add(s2);
            brush.GradientStops.Add(s3);

            overlay.Background = brush;

            // 插入到最底层
            rootPanel.Children.Insert(0, overlay);

            // 颜色呼吸动画（每个渐变停止点以不同速度变化，产生流动感）
            s1.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(
                Color.FromRgb(0x4A, 0xDE, 0x80), Color.FromRgb(0x2A, 0x9E, 0x60),
                TimeSpan.FromSeconds(4))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

            s2.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(
                Color.FromRgb(0x22, 0xD3, 0xEE), Color.FromRgb(0x1A, 0x7A, 0x9A),
                TimeSpan.FromSeconds(5))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

            s3.BeginAnimation(GradientStop.ColorProperty, new ColorAnimation(
                Color.FromRgb(0x7D, 0xD3, 0xA0), Color.FromRgb(0x3A, 0xBE, 0x80),
                TimeSpan.FromSeconds(6))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

            // 整体呼吸透明度
            overlay.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.08, 0.18, TimeSpan.FromSeconds(5))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });
        }

        /// <summary>
        /// 按钮点击脉冲反馈
        /// </summary>
        public static void ButtonPressPulse(Button button)
        {
            var pressDown = new DoubleAnimation(1, 0.85, TimeSpan.FromSeconds(0.03));
            pressDown.Completed += (_, _) =>
            {
                button.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.85, 1, TimeSpan.FromSeconds(0.15))
                {
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                });
            };
            button.BeginAnimation(UIElement.OpacityProperty, pressDown);
        }

        /// <summary>
        /// 元素进入闪烁高亮
        /// </summary>
        public static void Flash(Border element, Color flashColor, int times = 2)
        {
            if (element.Background is not SolidColorBrush originalBrush) return;
            var originalColor = originalBrush.Color;

            var flash = new ColorAnimation(originalColor, flashColor, TimeSpan.FromSeconds(0.1))
            {
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(times),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            element.Background.BeginAnimation(SolidColorBrush.ColorProperty, flash);
        }

        // ==================== 新增：丰富动画效果 ====================

        /// <summary>
        /// 持续呼吸脉冲动画（透明度）
        /// </summary>
        public static void Pulse(UIElement element, double from = 0.7, double to = 1.0, double durationSec = 1.5)
        {
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(from, to, TimeSpan.FromSeconds(durationSec))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });
        }

        /// <summary>
        /// 持续呼吸脉冲动画（缩放）
        /// </summary>
        public static void PulseScale(FrameworkElement element, double from = 1.0, double to = 1.05, double durationSec = 2.0)
        {
            if (element.RenderTransform == null || element.RenderTransform is not ScaleTransform)
            {
                element.RenderTransformOrigin = new Point(0.5, 0.5);
                element.RenderTransform = new ScaleTransform(1, 1);
            }
            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(from, to, TimeSpan.FromSeconds(durationSec))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });
            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(from, to, TimeSpan.FromSeconds(durationSec))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });
        }

        /// <summary>
        /// 横向扫光效果（Shimmer）— 在卡片上叠加一个渐变扫光层
        /// </summary>
        public static void Shimmer(Border targetCard, Color shimmerColor, double durationSec = 2.5)
        {
            var shimmer = new Border
            {
                IsHitTestVisible = false,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
                    {
                        new GradientStop(Colors.Transparent, 0.0),
                        new GradientStop(shimmerColor, 0.5),
                        new GradientStop(Colors.Transparent, 1.0)
                    }
                }
            };

            targetCard.CornerRadius = new CornerRadius(
                targetCard.CornerRadius.TopLeft > 0 ? targetCard.CornerRadius.TopLeft : 8);
            var originalClip = targetCard.Clip;
            shimmer.CornerRadius = targetCard.CornerRadius;

            targetCard.Child = shimmer; // 注意：这会替换原有内容！

            var fadeInOut = new DoubleAnimation(0, 0.25, TimeSpan.FromSeconds(durationSec))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            shimmer.BeginAnimation(UIElement.OpacityProperty, fadeInOut);
        }

        /// <summary>
        /// 滑动入场动画（从指定方向滑入 + 淡入）
        /// </summary>
        public static void SlideIn(FrameworkElement element, double fromX = 0, double fromY = 30,
            double durationSec = 0.4, double delaySec = 0)
        {
            element.Opacity = 0;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            var transform = new TranslateTransform(fromX, fromY);
            element.RenderTransform = transform;

            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(durationSec))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            transform.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(fromX, 0, TimeSpan.FromSeconds(durationSec))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            transform.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(fromY, 0, TimeSpan.FromSeconds(durationSec))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
        }

        /// <summary>
        /// 缩放淡入入场动画
        /// </summary>
        public static void ScaleIn(FrameworkElement element, double fromScale = 0.92, double durationSec = 0.4,
            double delaySec = 0)
        {
            element.Opacity = 0;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new ScaleTransform(fromScale, fromScale);

            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(durationSec))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(fromScale, 1, TimeSpan.FromSeconds(durationSec))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(fromScale, 1, TimeSpan.FromSeconds(durationSec))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
        }

        /// <summary>
        /// 背景渐变色缓慢流动动画
        /// </summary>
        public static void AnimateBackgroundGradient(Panel panel, Color color1, Color color2, double durationSec = 6.0)
        {
            // 必须先创建一个新的、未冻结的 SolidColorBrush，因为 StaticResource 的笔刷是冻结的
            var brush = new SolidColorBrush(color1);
            panel.Background = brush;

            brush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(color1, color2, TimeSpan.FromSeconds(durationSec))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });
        }

        /// <summary>
        /// 数字递增动画（TextBlock 内容从 from 到 to）
        /// </summary>
        public static void CountUp(TextBlock textBlock, int from, int to, string suffix = "",
            double durationMs = 800)
        {
            if (textBlock == null) return;
            int range = to - from;
            if (range == 0) { textBlock.Text = $"{to}{suffix}"; return; }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(16, durationMs / range)) };
            int current = from;
            timer.Tick += (s, e) =>
            {
                current++;
                textBlock.Text = $"{current}{suffix}";
                if (current >= to)
                {
                    timer.Stop();
                    textBlock.Text = $"{to}{suffix}";
                }
            };
            timer.Start();
        }

        /// <summary>
        /// 页面内容过渡：旧内容淡出 + 新内容交错滑入
        /// </summary>
        public static void PageTransition(Panel contentPanel, Action updateContent, double fadeOutMs = 100)
        {
            // 淡出当前内容
            var fadeOut = new DoubleAnimation(1, 0.3, TimeSpan.FromMilliseconds(fadeOutMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) =>
            {
                // 更新内容
                updateContent();

                // 交错滑入新内容
                contentPanel.Opacity = 1;
                int i = 0;
                foreach (UIElement child in contentPanel.Children)
                {
                    if (child is FrameworkElement fe && fe.IsVisible)
                    {
                        fe.Opacity = 0;
                        fe.RenderTransformOrigin = new Point(0.5, 0.5);
                        var tt = new TranslateTransform(0, 20);
                        fe.RenderTransform = tt;

                        int delay = i;
                        fe.BeginAnimation(UIElement.OpacityProperty,
                            new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                            {
                                BeginTime = TimeSpan.FromSeconds(0.05 * delay),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            });
                        tt.BeginAnimation(TranslateTransform.YProperty,
                            new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.35))
                            {
                                BeginTime = TimeSpan.FromSeconds(0.05 * delay),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            });
                        i++;
                    }
                }
            };
            contentPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        // ==================== 新增：高级动画效果 ====================

        /// <summary>
        /// 创建并启动一个漂浮粒子系统
        /// </summary>
        public static void CreateFloatingParticles(Canvas canvas, int count = 4,
            double minSize = 2, double maxSize = 5,
            string[]? colors = null, double durationMin = 6, double durationMax = 14)
        {
            if (colors == null || colors.Length == 0)
                colors = new[] { "#4ADE80", "#2DD4BF", "#A78BFA", "#60A5FA", "#FBBF24" };

            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                var color = colors[random.Next(colors.Length)];
                var size = minSize + random.NextDouble() * (maxSize - minSize);
                var dot = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = (Brush)new BrushConverter().ConvertFromString(color)!,
                    Opacity = 0,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                double startLeft = random.NextDouble() * (canvas.Width > 0 ? canvas.Width : 400);
                double startTop = canvas.Height > 0
                    ? canvas.Height + random.NextDouble() * 100
                    : 400 + random.NextDouble() * 200;
                double duration = durationMin + random.NextDouble() * (durationMax - durationMin);
                double delay = random.NextDouble() * durationMax;

                Canvas.SetLeft(dot, startLeft);
                Canvas.SetTop(dot, startTop);

                canvas.Children.Add(dot);

                // 上升动画
                var riseAnim = new DoubleAnimation(startTop, -50 - random.NextDouble() * 100,
                    TimeSpan.FromSeconds(duration))
                {
                    BeginTime = TimeSpan.FromSeconds(delay),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                dot.BeginAnimation(Canvas.TopProperty, riseAnim);

                // 淡入淡出
                var fadeIn = new DoubleAnimation(0, 0.15 + random.NextDouble() * 0.25,
                    TimeSpan.FromSeconds(duration * 0.15))
                {
                    BeginTime = TimeSpan.FromSeconds(delay)
                };
                var fadeOut = new DoubleAnimation(0.15 + random.NextDouble() * 0.25, 0,
                    TimeSpan.FromSeconds(duration * 0.15))
                {
                    BeginTime = TimeSpan.FromSeconds(delay + duration * 0.7)
                };

                dot.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(duration * 0.7)
                };
                int dotIndex = i; // capture
                EventHandler? tickHandler = null;
                tickHandler = (_, _) =>
                {
                    timer.Stop();
                    timer.Tick -= tickHandler;
                    dot.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                };
                timer.Tick += tickHandler;
                timer.Start();

                // 水平漂移
                if (random.NextDouble() > 0.5)
                {
                    var drift = new DoubleAnimation(
                        -20 + random.NextDouble() * 40,
                        TimeSpan.FromSeconds(duration * 0.5))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        BeginTime = TimeSpan.FromSeconds(delay)
                    };
                    dot.BeginAnimation(Canvas.LeftProperty, drift);
                }
            }
        }

        /// <summary>
        /// 按钮点击水波纹反馈
        /// </summary>
        public static void RippleEffect(Button button, Color rippleColor)
        {
            var ripple = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(rippleColor) { Opacity = 0.0 },
                IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0, 0)
            };

            var parent = button.Content as Panel ?? button.Parent as Panel;
            if (parent == null) return;

            // 计算点击位置
            Point mousePos = System.Windows.Input.Mouse.GetPosition(button);
            Canvas.SetLeft(ripple, mousePos.X - 5);
            Canvas.SetTop(ripple, mousePos.Y - 5);

            // 将 ripple 放入一个 overlay canvas
            var overlay = new Canvas { IsHitTestVisible = false };
            Grid.SetZIndex(overlay, 1000);
            overlay.Children.Add(ripple);

            if (parent is Grid grid)
            {
                grid.Children.Add(overlay);
            }
            else return;

            // 播放扩散动画
            var scaleUp = new DoubleAnimation(0, 3, TimeSpan.FromSeconds(0.5));
            scaleUp.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            ripple.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            ripple.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

            var fadeRipple = new DoubleAnimation(0.4, 0, TimeSpan.FromSeconds(0.5));
            fadeRipple.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            ripple.BeginAnimation(UIElement.OpacityProperty, fadeRipple);

            // 清理
            var cleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.6) };
            cleanupTimer.Tick += (_, _) =>
            {
                cleanupTimer.Stop();
                grid.Children.Remove(overlay);
            };
            cleanupTimer.Start();
        }

        /// <summary>
        /// 打字机效果
        /// </summary>
        public static void TypewriterText(TextBlock textBlock, string text,
            double intervalMs = 40, Action? onComplete = null)
        {
            textBlock.Text = "";
            int index = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
            timer.Tick += (_, _) =>
            {
                if (index < text.Length)
                {
                    textBlock.Text += text[index];
                    index++;
                }
                else
                {
                    timer.Stop();
                    onComplete?.Invoke();
                }
            };
            timer.Start();
        }

        /// <summary>
        /// 连续渐变背景动画
        /// </summary>
        public static void AnimatedGradientBackground(Grid target, Color[] colors,
            double durationPerTransition = 4.0)
        {
            if (colors.Length < 2) return;

            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                MappingMode = BrushMappingMode.RelativeToBoundingBox
            };

            for (int i = 0; i < colors.Length; i++)
            {
                brush.GradientStops.Add(new GradientStop(colors[i], i * 1.0 / (colors.Length - 1)));
            }

            target.Background = brush;

            int currentIndex = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationPerTransition) };
            timer.Tick += (_, _) =>
            {
                int nextIndex = (currentIndex + 1) % colors.Length;

                // 渐变漂移
                for (int i = 0; i < brush.GradientStops.Count; i++)
                {
                    float t = i * 1.0f / (brush.GradientStops.Count - 1);
                    int colorIndex = (int)(t * (colors.Length - 1));
                    var targetColor = colors[(colorIndex + 1) % colors.Length];

                    brush.GradientStops[i].BeginAnimation(
                        GradientStop.ColorProperty,
                        new ColorAnimation(brush.GradientStops[i].Color, targetColor,
                            TimeSpan.FromSeconds(durationPerTransition))
                        {
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                        });
                }

                currentIndex = nextIndex;
            };
            timer.Start();
        }

        /// <summary>
        /// 入场序列：子元素从不同方向交错飞入
        /// </summary>
        public static void StaggerFlyIn(Panel panel, string direction = "up",
            double distance = 40, double itemDelay = 0.06)
        {
            int i = 0;
            foreach (UIElement child in panel.Children)
            {
                if (child is FrameworkElement fe && fe.IsVisible)
                {
                    fe.Opacity = 0;
                    var tt = new TranslateTransform(0, 0);

                    double fromX = 0, fromY = 0;
                    switch (direction)
                    {
                        case "up": fromY = distance; break;
                        case "down": fromY = -distance; break;
                        case "left": fromX = distance; break;
                        case "right": fromX = -distance; break;
                    }

                    tt.X = fromX;
                    tt.Y = fromY;
                    fe.RenderTransform = tt;

                    int idx = i;
                    fe.BeginAnimation(UIElement.OpacityProperty,
                        new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                        {
                            BeginTime = TimeSpan.FromSeconds(itemDelay * idx),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        });

                    if (fromX != 0)
                        tt.BeginAnimation(TranslateTransform.XProperty,
                            new DoubleAnimation(fromX, 0, TimeSpan.FromSeconds(0.45))
                            {
                                BeginTime = TimeSpan.FromSeconds(itemDelay * idx),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            });
                    if (fromY != 0)
                        tt.BeginAnimation(TranslateTransform.YProperty,
                            new DoubleAnimation(fromY, 0, TimeSpan.FromSeconds(0.45))
                            {
                                BeginTime = TimeSpan.FromSeconds(itemDelay * idx),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            });
                }
                i++;
            }
        }

        /// <summary>
        /// 让窗口以弹性缩放动画关闭
        /// 注意：Style 中定义的 RenderTransform 是冻结的，不能走 Storyboard 属性路径。
        /// 解法：创建新的可变 ScaleTransform，直接 BeginAnimation。
        /// </summary>
        public static void WindowCloseWithScale(Window window, Action? onClosed = null)
        {
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            fade.Completed += (_, _) =>
            {
                onClosed?.Invoke();
                window.Close();
            };
            window.BeginAnimation(UIElement.OpacityProperty, fade);

            // 缩放动画：分层窗口上设置 ScaleTransform 可能失败，不影响关闭
            if (!window.AllowsTransparency)
            {
                try
                {
                    window.RenderTransform = new ScaleTransform(1, 1);
                    var scale = (ScaleTransform)window.RenderTransform;
                    var scaleDown = new DoubleAnimation(1, 0.8, TimeSpan.FromMilliseconds(150));
                    scaleDown.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
                }
                catch { /* 分层窗口不支持缩放动画，仅淡出 */ }
            }
        }

        // ==================== 新增：爆款动画效果集 ====================

        /// <summary>
        /// 骨架屏闪烁加载（Skeleton Shimmer）
        /// 在 Border 上创建一个扫光层，模拟内容加载中
        /// </summary>
        public static void SkeletonShimmer(Border target, Color shimmerColor, double durationSec = 1.8)
        {
            var overlay = new Border
            {
                IsHitTestVisible = false,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                CornerRadius = target.CornerRadius,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops =
                    {
                        new GradientStop(Colors.Transparent, 0.0),
                        new GradientStop(Colors.Transparent, 0.3),
                        new GradientStop(shimmerColor, 0.5),
                        new GradientStop(Colors.Transparent, 0.7),
                        new GradientStop(Colors.Transparent, 1.0)
                    }
                }
            };

            target.Child = overlay; // Replace child with shimmer

            var fadeIn = new DoubleAnimation(0, 0.2, TimeSpan.FromSeconds(durationSec * 0.3));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            var fadeOut = new DoubleAnimation(0.2, 0, TimeSpan.FromSeconds(durationSec * 0.3));
            fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            fadeOut.BeginTime = TimeSpan.FromSeconds(durationSec * 0.7);

            overlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSec * 0.7) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                overlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                var cleanup = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSec * 0.3 + 0.1) };
                cleanup.Tick += (_, _) =>
                {
                    cleanup.Stop();
                    if (target.Child == overlay) target.Child = null;
                };
                cleanup.Start();
            };
            timer.Start();
        }

        /// <summary>
        /// 磁吸悬浮效果 - 按钮靠近鼠标时轻微放大+光晕
        /// 注意：必须处理 Style 中冻结的 RenderTransform（WPF 冻结对象无法动画化）
        /// </summary>
        public static void AttachMagneticHover(FrameworkElement element, double scaleTo = 1.04)
        {
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            // 如果 RenderTransform 为空、不是 ScaleTransform、或被冻结，全部替换为新的可变实例
            if (element.RenderTransform == null
                || element.RenderTransform is not ScaleTransform
                || element.RenderTransform.IsFrozen)
            {
                element.RenderTransform = new ScaleTransform(1, 1);
            }

            element.MouseEnter += (_, _) =>
            {
                try
                {
                    element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                        new DoubleAnimation(1, scaleTo, TimeSpan.FromMilliseconds(200))
                        { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
                    element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                        new DoubleAnimation(1, scaleTo, TimeSpan.FromMilliseconds(200))
                        { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
                }
                catch { /* 静默动画失败 */ }
            };
            element.MouseLeave += (_, _) =>
            {
                try
                {
                    element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                        new DoubleAnimation(scaleTo, 1, TimeSpan.FromMilliseconds(200))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                        new DoubleAnimation(scaleTo, 1, TimeSpan.FromMilliseconds(200))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
                catch { /* 静默动画失败 */ }
            };
        }

        /// <summary>
        /// 持续浮动动画（上下漂浮）
        /// </summary>
        public static void FloatingAnimation(FrameworkElement element, double distance = 8,
            double durationSec = 3, double delaySec = 0)
        {
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new TranslateTransform(0, 0);

            element.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(-distance, distance, TimeSpan.FromSeconds(durationSec))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });
        }

        /// <summary>
        /// 弹性弹跳入场
        /// </summary>
        public static void BounceIn(FrameworkElement element, double fromScale = 0.5,
            double durationSec = 0.6, double delaySec = 0)
        {
            element.Opacity = 0;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new ScaleTransform(fromScale, fromScale);

            element.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(durationSec * 0.3))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(fromScale, 1.1, TimeSpan.FromSeconds(durationSec * 0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(fromScale, 1.1, TimeSpan.FromSeconds(durationSec * 0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            // Overshoot back
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSec * 0.4 + delaySec) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1.1, 1, TimeSpan.FromSeconds(durationSec * 0.3))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(1.1, 1, TimeSpan.FromSeconds(durationSec * 0.3))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            };
            timer.Start();
        }

        /// <summary>
        /// 旋转入场（3D翻转感）
        /// </summary>
        public static void RotateIn(FrameworkElement element, double fromAngle = -15,
            double durationSec = 0.5, double delaySec = 0)
        {
            element.Opacity = 0;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new RotateTransform(fromAngle);

            element.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(durationSec * 0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            element.RenderTransform.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(fromAngle, 0, TimeSpan.FromSeconds(durationSec))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                });
        }

        /// <summary>
        /// 连续呼吸光晕脉冲（用于重要元素，如状态指示灯、徽章）
        /// </summary>
        public static void AttachGlowPulse(FrameworkElement element, Color glowColor,
            double minOpacity = 0.3, double maxOpacity = 0.7, double durationSec = 2)
        {
            var effect = new DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 0,
                Color = glowColor,
                Opacity = minOpacity
            };
            element.Effect = effect;

            effect.BeginAnimation(DropShadowEffect.OpacityProperty,
                new DoubleAnimation(minOpacity, maxOpacity, TimeSpan.FromSeconds(durationSec))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });
            effect.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
                new DoubleAnimation(16, 28, TimeSpan.FromSeconds(durationSec))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });
        }

        /// <summary>
        /// 带动画地展示 Toast 通知（从上方滑入）
        /// </summary>
        public static Border ShowToastNotification(Panel parent, string message,
            Brush? accentBrush = null, double durationSec = 3.5)
        {
            accentBrush ??= new SolidColorBrush(Color.FromRgb(74, 222, 128));

            var toast = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 46, 31)),
                BorderBrush = accentBrush,
                BorderThickness = new Thickness(0, 2, 0, 0),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 14, 20, 14),
                Margin = new Thickness(16, 16, 16, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = 0,
                RenderTransform = new TranslateTransform(0, -60),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 20, ShadowDepth = 4,
                    Color = Colors.Black, Opacity = 0.4
                }
            };

            var text = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };
            toast.Child = text;

            Panel.SetZIndex(toast, 1000);
            parent.Children.Add(toast);

            // Slide in
            toast.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            toast.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(-60, 0, TimeSpan.FromSeconds(0.4))
                { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });

            // Auto dismiss
            var dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSec) };
            dismissTimer.Tick += (_, _) =>
            {
                dismissTimer.Stop();
                toast.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
                toast.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(0, -40, TimeSpan.FromSeconds(0.3))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });

                var cleanup = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.4) };
                cleanup.Tick += (_, _) =>
                {
                    cleanup.Stop();
                    parent.Children.Remove(toast);
                };
                cleanup.Start();
            };
            dismissTimer.Start();

            return toast;
        }

        /// <summary>
        /// 数字跳动计数动画
        /// </summary>
        public static void AnimateCounter(TextBlock textBlock, int from, int to,
            string format = "{0}", double durationMs = 800)
        {
            int range = Math.Abs(to - from);
            if (range == 0) { textBlock.Text = string.Format(format, to); return; }

            int direction = to > from ? 1 : -1;
            int current = from;
            int step = Math.Max(1, range / 30);
            double intervalMs = Math.Max(16, durationMs / (range / step));

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
            timer.Tick += (_, _) =>
            {
                current += direction * step;
                if ((direction > 0 && current >= to) || (direction < 0 && current <= to))
                {
                    current = to;
                    timer.Stop();
                }
                textBlock.Text = string.Format(format, current);
            };
            timer.Start();
        }

        /// <summary>
        /// 内容渐入切换（先淡出→更新内容→淡入）
        /// </summary>
        public static void CrossfadeContent(FrameworkElement container, Action updateContent,
            double fadeDuration = 0.15)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(fadeDuration));
            fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
            fadeOut.Completed += (_, _) =>
            {
                updateContent();
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(fadeDuration * 1.5));
                fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                container.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };
            container.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        // ==================== anime.js 风格动画 ====================

        /// <summary>
        /// anime.js 风格弹性弹跳（三段式：overshoot → settle → rest）
        /// 比 BackEase 更弹、更夸张、更有 anime.js 的味道
        /// </summary>
        public static void ElasticBounce(FrameworkElement element,
            double fromScale = 0.3, double overshoot = 1.15,
            double durationSec = 0.8, double delaySec = 0)
        {
            element.Opacity = 0;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new ScaleTransform(fromScale, fromScale);

            // Phase 1: 淡入 + 快速弹出到 overshoot
            element.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(durationSec * 0.25))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(fromScale, overshoot, TimeSpan.FromSeconds(durationSec * 0.35))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(fromScale, overshoot, TimeSpan.FromSeconds(durationSec * 0.35))
                {
                    BeginTime = TimeSpan.FromSeconds(delaySec),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            // Phase 2: overshoot → undershoot
            double t2 = durationSec * 0.35 + delaySec;
            var timer1 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(t2) };
            timer1.Tick += (_, _) =>
            {
                timer1.Stop();
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(overshoot, 0.92, TimeSpan.FromSeconds(durationSec * 0.25))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(overshoot, 0.92, TimeSpan.FromSeconds(durationSec * 0.25))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            };
            timer1.Start();

            // Phase 3: undershoot → 1.0 settle
            double t3 = durationSec * 0.6 + delaySec;
            var timer2 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(t3) };
            timer2.Tick += (_, _) =>
            {
                timer2.Stop();
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(durationSec * 0.25))
                    { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(durationSec * 0.25))
                    { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
            };
            timer2.Start();
        }

        /// <summary>
        /// anime.js 风格时间线：按顺序执行多个动画步骤
        /// 每个步骤可以指定延迟、持续时间和要执行的操作
        /// </summary>
        public class TimelineStep
        {
            public string TargetProperty { get; set; } = "Opacity";
            public double From { get; set; }
            public double To { get; set; }
            public double DurationMs { get; set; } = 300;
            public double DelayMs { get; set; } = 0;
            public IEasingFunction? Easing { get; set; }
            public Action? OnStart { get; set; }
            public Action? OnComplete { get; set; }
        }

        /// <summary>
        /// 对元素播放 anime.js 风格的时间线动画
        /// </summary>
        public static void PlayTimeline(FrameworkElement element, List<TimelineStep> steps)
        {
            double cumulativeDelay = 0;
            foreach (var step in steps)
            {
                double startTime = cumulativeDelay + step.DelayMs;
                double duration = step.DurationMs;

                if (step.TargetProperty == "Opacity")
                {
                    var anim = new DoubleAnimation(step.From, step.To, TimeSpan.FromMilliseconds(duration))
                    {
                        BeginTime = TimeSpan.FromMilliseconds(startTime),
                        EasingFunction = step.Easing ?? new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    if (step.OnComplete != null)
                        anim.Completed += (_, _) => step.OnComplete();
                    element.BeginAnimation(UIElement.OpacityProperty, anim);
                }
                else if (step.TargetProperty == "ScaleX" || step.TargetProperty == "ScaleY")
                {
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                    if (element.RenderTransform == null || element.RenderTransform is not ScaleTransform)
                        element.RenderTransform = new ScaleTransform(1, 1);

                    var dep = step.TargetProperty == "ScaleX"
                        ? ScaleTransform.ScaleXProperty
                        : ScaleTransform.ScaleYProperty;

                    var anim = new DoubleAnimation(step.From, step.To, TimeSpan.FromMilliseconds(duration))
                    {
                        BeginTime = TimeSpan.FromMilliseconds(startTime),
                        EasingFunction = step.Easing ?? new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    if (step.OnComplete != null)
                        anim.Completed += (_, _) => step.OnComplete();
                    element.RenderTransform.BeginAnimation(dep, anim);
                }
                else if (step.TargetProperty == "TranslateX")
                {
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                    if (element.RenderTransform == null || element.RenderTransform is not TranslateTransform)
                        element.RenderTransform = new TranslateTransform(0, 0);

                    var anim = new DoubleAnimation(step.From, step.To, TimeSpan.FromMilliseconds(duration))
                    {
                        BeginTime = TimeSpan.FromMilliseconds(startTime),
                        EasingFunction = step.Easing ?? new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    element.RenderTransform.BeginAnimation(TranslateTransform.XProperty, anim);
                }
                else if (step.TargetProperty == "TranslateY")
                {
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                    if (element.RenderTransform == null || element.RenderTransform is not TranslateTransform)
                        element.RenderTransform = new TranslateTransform(0, 0);

                    var anim = new DoubleAnimation(step.From, step.To, TimeSpan.FromMilliseconds(duration))
                    {
                        BeginTime = TimeSpan.FromMilliseconds(startTime),
                        EasingFunction = step.Easing ?? new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    element.RenderTransform.BeginAnimation(TranslateTransform.YProperty, anim);
                }

                cumulativeDelay += step.DelayMs + step.DurationMs;
            }
        }

        /// <summary>
        /// anime.js 风格交错分组动画
        /// 每个子元素以递增 delay 播放动画，营造波浪效果
        /// </summary>
        public static void AnimeStagger(UIElementCollection children,
            string direction = "up", double distance = 35,
            double baseDelay = 0.3, double itemDelay = 0.06,
            double durationSec = 0.4)
        {
            int i = 0;
            foreach (UIElement child in children)
            {
                if (child is FrameworkElement fe && fe.IsVisible)
                {
                    double fromX = 0, fromY = 0;
                    switch (direction)
                    {
                        case "up": fromY = distance; break;
                        case "down": fromY = -distance; break;
                        case "left": fromX = distance; break;
                        case "right": fromX = -distance; break;
                    }

                    double delay = baseDelay + itemDelay * i;
                    fe.Opacity = 0;
                    var tt = new TranslateTransform(fromX, fromY);
                    fe.RenderTransform = tt;

                    // 透明度
                    fe.BeginAnimation(UIElement.OpacityProperty,
                        new DoubleAnimation(0, 1, TimeSpan.FromSeconds(durationSec))
                        {
                            BeginTime = TimeSpan.FromSeconds(delay),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        });

                    // 位移动画使用 elastic easing
                    if (fromX != 0)
                        tt.BeginAnimation(TranslateTransform.XProperty,
                            new DoubleAnimation(fromX, 0, TimeSpan.FromSeconds(durationSec))
                            {
                                BeginTime = TimeSpan.FromSeconds(delay),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            });
                    if (fromY != 0)
                        tt.BeginAnimation(TranslateTransform.YProperty,
                            new DoubleAnimation(fromY, 0, TimeSpan.FromSeconds(durationSec))
                            {
                                BeginTime = TimeSpan.FromSeconds(delay),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                            });
                }
                i++;
            }
        }

        /// <summary>
        /// anime.js 风格 Scale-Y 弹性跳动（配合数字增长，类似计数器跳动）
        /// </summary>
        public static void NumberPop(TextBlock textBlock, double fromScale = 1.6)
        {
            textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
            textBlock.RenderTransform = new ScaleTransform(fromScale, fromScale);

            textBlock.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(fromScale, 1, TimeSpan.FromSeconds(0.5))
                { EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 4, EasingMode = EasingMode.EaseOut } });
            textBlock.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(fromScale, 1, TimeSpan.FromSeconds(0.5))
                { EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 4, EasingMode = EasingMode.EaseOut } });
        }

        // ==================== Uiverse.io 风格动画 ====================

        /// <summary>
        /// Uiverse 脉冲环：按钮周围扩散光环（太空/赛博朋克风格）
        /// </summary>
        public static void PulseRing(Ellipse ring, double fromScale = 0.1, double toScale = 1.8,
            double durationSec = 1.2, double delaySec = 0, bool repeat = true)
        {
            if (ring.RenderTransform == null || ring.RenderTransform is not ScaleTransform)
            {
                ring.RenderTransformOrigin = new Point(0.5, 0.5);
                ring.RenderTransform = new ScaleTransform(fromScale, fromScale);
            }

            var scaleUp = new DoubleAnimation(fromScale, toScale, TimeSpan.FromSeconds(durationSec))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                AutoReverse = true,
                RepeatBehavior = repeat ? RepeatBehavior.Forever : new RepeatBehavior(1)
            };

            var fadeOut = new DoubleAnimation(0.6, 0, TimeSpan.FromSeconds(durationSec))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                AutoReverse = true,
                RepeatBehavior = repeat ? RepeatBehavior.Forever : new RepeatBehavior(1)
            };

            ring.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            ring.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
            ring.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Uiverse 3D 卡片倾斜：鼠标位置驱动卡片向光源方向倾斜
        /// 在卡片上产生真实 3D 光照跟随效果
        /// </summary>
        public static void Attach3DTilt(FrameworkElement card, FrameworkElement? lightSource = null,
            double maxAngle = 5.0, double scaleOnHover = 1.03)
        {
            var tiltX = new RotateTransform(0);
            var tiltY = new RotateTransform(0);

            // 用两个 RotateTransform 组实现 X/Y 轴倾斜
            card.RenderTransformOrigin = new Point(0.5, 0.5);

            card.MouseEnter += (_, _) =>
            {
                if (card.RenderTransform is not ScaleTransform)
                {
                    card.RenderTransform = new ScaleTransform(1, 1);
                }
                card.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1, scaleOnHover, TimeSpan.FromMilliseconds(200))
                    { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
                card.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(1, scaleOnHover, TimeSpan.FromMilliseconds(200))
                    { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
            };

            card.MouseMove += (s, e) =>
            {
                try
                {
                    var pos = e.GetPosition(card);
                    double w = card.ActualWidth;
                    double h = card.ActualHeight;
                    if (w <= 1 || h <= 1) return;

                    double angleX = (pos.Y / h - 0.5) * maxAngle;   // 上下倾斜
                    double angleY = (pos.X / w - 0.5) * -maxAngle;   // 左右倾斜（反向）

                    tiltX.BeginAnimation(RotateTransform.AngleProperty,
                        new DoubleAnimation(angleX, TimeSpan.FromMilliseconds(80))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    tiltY.BeginAnimation(RotateTransform.AngleProperty,
                        new DoubleAnimation(angleY, TimeSpan.FromMilliseconds(80))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
                catch { }
            };

            card.MouseLeave += (_, _) =>
            {
                tiltX.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(0, TimeSpan.FromMilliseconds(250))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                tiltY.BeginAnimation(RotateTransform.AngleProperty,
                    new DoubleAnimation(0, TimeSpan.FromMilliseconds(250))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

                if (card.RenderTransform is ScaleTransform st)
                {
                    st.BeginAnimation(ScaleTransform.ScaleXProperty,
                        new DoubleAnimation(scaleOnHover, 1, TimeSpan.FromMilliseconds(250))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    st.BeginAnimation(ScaleTransform.ScaleYProperty,
                        new DoubleAnimation(scaleOnHover, 1, TimeSpan.FromMilliseconds(250))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
            };
        }

        /// <summary>
        /// Uiverse Glitch 文字效果：文字闪烁错位（赛博朋克故障风格）
        /// </summary>
        public static void GlitchEffect(TextBlock textBlock, double durationSec = 0.15, int times = 3)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            int count = 0;
            string originalText = textBlock.Text;
            double originalFontSize = textBlock.FontSize;

            timer.Tick += (_, _) =>
            {
                count++;
                if (count > times * 3)
                {
                    timer.Stop();
                    textBlock.Text = originalText;
                    textBlock.Opacity = 1;
                    textBlock.FontSize = originalFontSize;
                    textBlock.BeginAnimation(UIElement.OpacityProperty,
                        new DoubleAnimation(0.6, 1, TimeSpan.FromSeconds(0.1))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                    return;
                }

                // 随机故障效果
                var rng = new Random();
                textBlock.Opacity = 0.5 + rng.NextDouble() * 0.5;
                textBlock.FontSize = originalFontSize + (rng.NextDouble() - 0.5) * 4;
                if (rng.NextDouble() > 0.6)
                {
                    char[] chars = originalText.ToCharArray();
                    int idx = rng.Next(chars.Length);
                    chars[idx] = (char)(33 + rng.Next(94)); // 随机ASCII字符
                    textBlock.Text = new string(chars);
                }
                else
                {
                    textBlock.Text = originalText;
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Uiverse 聚光灯效果：鼠标进入时径向渐变从鼠标位置扩散
        /// </summary>
        public static void AttachSpotlight(Border card, Color glowColor)
        {
            var overlay = new Border
            {
                IsHitTestVisible = false,
                Opacity = 0,
                CornerRadius = card.CornerRadius,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var radialBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                RadiusX = 1.5,
                RadiusY = 1.5,
                MappingMode = BrushMappingMode.RelativeToBoundingBox,
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0x30, glowColor.R, glowColor.G, glowColor.B), 0.0),
                    new GradientStop(Colors.Transparent, 0.7)
                }
            };
            overlay.Background = radialBrush;

            // 插入到卡片内
            if (card.Child is Grid g)
            {
                overlay.SetValue(Grid.ZIndexProperty, 999);
                g.Children.Add(overlay);
            }

            card.MouseMove += (s, e) =>
            {
                try
                {
                    var pos = e.GetPosition(card);
                    double rx = pos.X / card.ActualWidth;
                    double ry = pos.Y / card.ActualHeight;
                    radialBrush.GradientOrigin = new Point(rx, ry);
                    radialBrush.Center = new Point(rx, ry);
                    overlay.BeginAnimation(UIElement.OpacityProperty,
                        new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                }
                catch { }
            };

            card.MouseLeave += (_, _) =>
            {
                overlay.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
            };
        }

        /// <summary>
        /// Uiverse Confetti 粒子爆发：庆祝/成功反馈（彩色方块粒子爆炸）
        /// </summary>
        public static void ParticleBurst(Panel parent, Point center, int count = 20,
            double durationSec = 1.0)
        {
            var rng = new Random();
            Color[] palette =
            {
                Color.FromRgb(0x4A, 0xDE, 0x80),
                Color.FromRgb(0x2D, 0xD4, 0xBF),
                Color.FromRgb(0xA7, 0x8B, 0xFA),
                Color.FromRgb(0x60, 0xA5, 0xFA),
                Color.FromRgb(0xFB, 0xBF, 0x24),
                Color.FromRgb(0xFB, 0x71, 0x85),
            };

            for (int i = 0; i < count; i++)
            {
                var particle = new Ellipse
                {
                    Width = 4 + rng.NextDouble() * 6,
                    Height = 4 + rng.NextDouble() * 6,
                    Fill = new SolidColorBrush(palette[rng.Next(palette.Length)]),
                    Opacity = 1,
                    IsHitTestVisible = false,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new TranslateTransform(center.X, center.Y)
                };
                parent.Children.Add(particle);

                double angle = rng.NextDouble() * 360;
                double distance = 40 + rng.NextDouble() * 100;
                double endX = center.X + Math.Cos(angle * Math.PI / 180) * distance;
                double endY = center.Y + Math.Sin(angle * Math.PI / 180) * distance;
                double duration = 0.5 + rng.NextDouble() * durationSec;

                particle.RenderTransform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(center.X, endX, TimeSpan.FromSeconds(duration))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                particle.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(center.Y, endY, TimeSpan.FromSeconds(duration))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                particle.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromSeconds(duration))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });

                // Cleanup
                var cleanup = new DispatcherTimer { Interval = TimeSpan.FromSeconds(duration + 0.1) };
                int captureI = i;
                EventHandler? tick = null;
                tick = (_, _) =>
                {
                    cleanup.Stop();
                    cleanup.Tick -= tick;
                    parent.Children.Remove(particle);
                };
                cleanup.Tick += tick;
                cleanup.Start();
            }
        }

        /// <summary>
        /// Uiverse Shimmer 扫光：在元素上从左到右扫过一条高光线
        /// </summary>
        public static void ShimmerSweep(Border target, double durationSec = 1.5, double delaySec = 0)
        {
            var shimmer = new Border
            {
                IsHitTestVisible = false,
                Width = 60,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 0,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops =
                    {
                        new GradientStop(Colors.Transparent, 0.0),
                        new GradientStop(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF), 0.5),
                        new GradientStop(Colors.Transparent, 1.0)
                    }
                }
            };

            if (target.Child is Grid g)
            {
                shimmer.SetValue(Grid.ZIndexProperty, 1000);
                g.Children.Add(shimmer);
            }
            else return;

            var fadeIn = new DoubleAnimation(0, 0.6, TimeSpan.FromSeconds(durationSec * 0.1))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slide = new DoubleAnimation(-60, target.ActualWidth + 60,
                TimeSpan.FromSeconds(durationSec))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec + durationSec * 0.1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var fadeOut = new DoubleAnimation(0.6, 0, TimeSpan.FromSeconds(durationSec * 0.15))
            {
                BeginTime = TimeSpan.FromSeconds(delaySec + durationSec * 0.1 + durationSec),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            shimmer.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            shimmer.BeginAnimation(Canvas.LeftProperty, slide);

            // Cleanup
            var cleanup = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(delaySec + durationSec + 0.3)
            };
            cleanup.Tick += (_, _) =>
            {
                cleanup.Stop();
                g.Children.Remove(shimmer);
            };
            cleanup.Start();
        }

        /// <summary>
        /// Uiverse 弹性弹跳文本：每个字符依次弹跳（类似字母跳跃）
        /// </summary>
        public static void TextBounceIn(TextBlock textBlock, double itemDelay = 0.03,
            double bounceHeight = -8)
        {
            string text = textBlock.Text;
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            for (int i = 0; i < text.Length; i++)
            {
                var charBlock = new TextBlock
                {
                    Text = text[i].ToString(),
                    FontSize = textBlock.FontSize,
                    FontWeight = textBlock.FontWeight,
                    Foreground = textBlock.Foreground,
                    FontFamily = textBlock.FontFamily,
                    Opacity = 0,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new TranslateTransform(0, bounceHeight)
                };

                int idx = i;
                charBlock.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15))
                    { BeginTime = TimeSpan.FromSeconds(itemDelay * idx) });

                charBlock.RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(bounceHeight, 0, TimeSpan.FromSeconds(0.4))
                    {
                        BeginTime = TimeSpan.FromSeconds(itemDelay * idx),
                        EasingFunction = new ElasticEase
                        {
                            Oscillations = 1, Springiness = 3,
                            EasingMode = EasingMode.EaseOut
                        }
                    });

                panel.Children.Add(charBlock);
            }

            var parent = textBlock.Parent as Panel;
            if (parent != null)
            {
                int index = parent.Children.IndexOf(textBlock);
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, panel);
            }
        }

        /// <summary>
        /// Uiverse 霓虹边框脉冲：边框颜色呼吸切换
        /// </summary>
        public static void NeonBorderPulse(Border border, Color color1, Color color2,
            double durationSec = 2.0)
        {
            var brush = new SolidColorBrush(color1);
            border.BorderBrush = brush;

            brush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(color1, color2, TimeSpan.FromSeconds(durationSec))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });

            var effect = border.Effect as DropShadowEffect;
            if (effect != null)
            {
                effect.BeginAnimation(DropShadowEffect.ColorProperty,
                    new ColorAnimation(color1, color2, TimeSpan.FromSeconds(durationSec))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever,
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    });
            }
        }

        /// <summary>
        /// Uiverse 输入框聚焦波纹：聚焦时从输入框底边展开彩色下划线
        /// </summary>
        public static void AttachFocusUnderline(TextBox textBox,
            Color focusColor)
        {
            var underline = new Border
            {
                Height = 2,
                Width = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(focusColor),
                CornerRadius = new CornerRadius(1),
                Opacity = 0,
                Margin = new Thickness(0, 0, 0, -1)
            };

            textBox.Loaded += (_, _) =>
            {
                var parent = textBox.Parent as Grid;
                parent?.Children.Add(underline);
            };

            textBox.GotFocus += (_, _) =>
            {
                underline.BeginAnimation(FrameworkElement.WidthProperty,
                    new DoubleAnimation(0, textBox.ActualWidth, TimeSpan.FromSeconds(0.35))
                    { EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
                underline.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2)));
            };

            textBox.LostFocus += (_, _) =>
            {
                underline.BeginAnimation(FrameworkElement.WidthProperty,
                    new DoubleAnimation(textBox.ActualWidth, 0, TimeSpan.FromSeconds(0.25))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
                underline.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2)));
            };
        }
    }
}
