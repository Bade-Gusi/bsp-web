using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BeiShuiCS2.Services;

namespace BeiShuiCS2
{
    public partial class HolidayPopupWindow : Window
    {
        private readonly HolidayInfo _holiday;
        private bool _dismissToday;

        public HolidayPopupWindow(HolidayInfo holiday)
        {
            InitializeComponent();
            _holiday = holiday;

            Loaded += (s, e) =>
            {
                txtEmoji.Text = holiday.Emoji;
                txtTitle.Text = $"{holiday.Name}快乐";
                txtMessage.Text = holiday.Message;

                var (c1, c2) = HolidayService.ParseColors(holiday.Color1, holiday.Color2);
                gradStop1.Color = c1;
                gradStop2.Color = c2;

                PlayEntranceSequence();
            };
        }

        private void PlayEntranceSequence()
        {
            // Phase 1: 窗口整体弹性弹入
            var scaleUp = new DoubleAnimation(0.7, 1.0, TimeSpan.FromSeconds(0.7));
            scaleUp.EasingFunction = new ElasticEase
            {
                Oscillations = 2,
                Springiness = 5,
                EasingMode = EasingMode.EaseOut
            };
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
            cardContainer.BeginAnimation(OpacityProperty, fadeIn);
            cardContainer.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            cardContainer.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

            // Phase 2: Emoji 圆环弹性弹出（延迟）
            var emojiScale = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6));
            emojiScale.BeginTime = TimeSpan.FromSeconds(0.3);
            emojiScale.EasingFunction = new BackEase
            {
                Amplitude = 0.5,
                EasingMode = EasingMode.EaseOut
            };
            emojiCircle.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, emojiScale);
            emojiCircle.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, emojiScale);

            // Phase 3: Emoji 呼吸脉冲
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.9) };
            t.Tick += (_, _) =>
            {
                t.Stop();
                var pulse = new DoubleAnimation(0.92, 1.08, TimeSpan.FromSeconds(2.0));
                pulse.AutoReverse = true;
                pulse.RepeatBehavior = RepeatBehavior.Forever;
                emojiCircle.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
                emojiCircle.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
            };
            t.Start();

            // Phase 4: 标题文字淡入上滑
            var titleFade = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            titleFade.BeginTime = TimeSpan.FromSeconds(0.6);
            txtTitle.BeginAnimation(OpacityProperty, titleFade);

            // Phase 5: 祝福语淡入（按段落逐行）
            var msgFade = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6));
            msgFade.BeginTime = TimeSpan.FromSeconds(1.0);
            txtMessage.BeginAnimation(OpacityProperty, msgFade);

            // Phase 6: 底部按钮淡入
            var footerAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
            footerAnim.BeginTime = TimeSpan.FromSeconds(1.5);
            bottomButtons.BeginAnimation(OpacityProperty, footerAnim);

            // Phase 7: 撒花粒子
            var t2 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
            t2.Tick += (_, _) =>
            {
                t2.Stop();
                try { AnimationHelper.ParticleBurst(particleCanvas, new System.Windows.Point(250, 300), count: 50); }
                catch { }
            };
            t2.Start();
        }

        private void DismissToday_Click(object sender, RoutedEventArgs e)
        {
            _dismissToday = true;
            CloseWithAnimation();
        }

        private void DismissPermanent_Click(object sender, RoutedEventArgs e)
        {
            var settings = AppSettings.Load();
            settings.LastDismissedHoliday = "PERMANENT_" + _holiday.Name;
            settings.Save();
            CloseWithAnimation();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CloseWithAnimation();
        }

        private void CloseWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            var scaleDown = new DoubleAnimation(1, 0.85, TimeSpan.FromSeconds(0.25));
            scaleDown.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };

            fadeOut.Completed += (_, _) =>
            {
                if (_dismissToday)
                {
                    var settings = AppSettings.Load();
                    settings.LastDismissedHoliday = $"{DateTime.Now:yyyy-MM-dd}_{_holiday.Name}";
                    settings.Save();
                }
                DialogResult = true;
                Close();
            };

            cardContainer.BeginAnimation(OpacityProperty, fadeOut);
            cardContainer.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
            cardContainer.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
        }
    }
}
