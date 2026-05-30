using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class AntiCheatStatusWindow : Window
    {
        public AntiCheatStatusWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 64) this.DragMove();
            };
            this.Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            };
        }

        private void HeartbeatCheck_Click(object sender, RoutedEventArgs e)
        {
            var heartbeat = new AntiCheatHeartbeatWindow { Owner = this };
            bool? result = heartbeat.ShowDialog();

            if (result == true)
            {
                var mainWin = Application.Current.MainWindow as MainWindow;
                mainWin?.ShowToast("✅ 心跳验证通过，系统运行正常");
            }
            else
            {
                var mainWin = Application.Current.MainWindow as MainWindow;
                mainWin?.ShowToast("⚠️ 心跳验证未响应");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
