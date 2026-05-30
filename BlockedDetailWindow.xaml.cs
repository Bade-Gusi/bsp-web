using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class BlockedDetailWindow : Window
    {
        public BlockedDetailWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

                // 填充封锁详情
                txtBlockTime.Text = App.LastAntiCheatScan != DateTime.MinValue
                    ? App.LastAntiCheatScan.ToString("yyyy-MM-dd HH:mm:ss")
                    : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                txtBlockReason.Text = App.AntiCheatPassed
                    ? "系统检测到异常行为"
                    : "反作弊扫描未通过";

                // 填充日志
                string log = "反作弊系统扫描日志：\n\n";
                log += $"{App.LastAntiCheatScan:yyyy-MM-dd HH:mm:ss} [错误] 反作弊检查未通过\n";
                log += $"{App.LastAntiCheatScan.AddMinutes(-1):yyyy-MM-dd HH:mm:ss} [警告] 检测到系统环境异常\n";
                log += $"{App.LastAntiCheatScan.AddMinutes(-2):yyyy-MM-dd HH:mm:ss} [信息] 触发安全检查机制\n";
                log += $"{App.LastAntiCheatScan.AddMinutes(-3):yyyy-MM-dd HH:mm:ss} [信息] 开始定期安全检查";
                txtLog.Text = log;
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
