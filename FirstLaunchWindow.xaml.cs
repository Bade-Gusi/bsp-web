using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class FirstLaunchWindow : Window
    {
        public FirstLaunchWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.GetPosition(this).Y < 60) this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AnimationHelper.CreateFloatingParticles(particleCanvas, 18,
                minSize: 2, maxSize: 4,
                colors: new[] { "#4ADE80", "#2DD4BF", "#A78BFA", "#60A5FA" },
                durationMin: 8, durationMax: 20);
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5)));

            // 首先显示欢迎界面，延迟执行入场动画
            AnimateStaggeredEntrance();
        }

        private void AnimateStaggeredEntrance()
        {
            // Logo 弹性入场
            if (headerSection != null)
            {
                AnimationHelper.ElasticBounce(headerSection, fromScale: 0.4, overshoot: 1.12, durationSec: 1.0);
            }

            var items = new FrameworkElement[] { contentPanel, footerSection, chkDontShow };
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                items[i].Opacity = 0;
                items[i].RenderTransformOrigin = new Point(0.5, 0.5);
                items[i].RenderTransform = new TranslateTransform(0, 30);

                int idx = i;
                items[i].BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                    { BeginTime = TimeSpan.FromSeconds(0.3 + idx * 0.12), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

                items[i].RenderTransform.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(30, 0, TimeSpan.FromSeconds(0.5))
                    { BeginTime = TimeSpan.FromSeconds(0.3 + idx * 0.12), EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut } });
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (chkDontShow.IsChecked == true)
            {
                var settings = AppSettings.Load();
                settings.IsFirstLaunch = false;
                settings.Save();
            }

            // 先弹出服务器配置窗口，确认地址后再到登录窗口
            var configWin = new ServerConfigWindow { Owner = this };
            if (configWin.ShowDialog() == true)
            {
                AnimationHelper.WindowCloseWithScale(this, () =>
                {
                    var login = new LoginWindow();
                    login.Show();
                });
            }
        }
    }
}
