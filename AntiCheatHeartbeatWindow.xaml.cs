using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class AntiCheatHeartbeatWindow : Window
    {
        private DispatcherTimer? _statusUpdateTimer;
        private readonly ObservableCollection<string> _logEntries = new();
        private readonly Brush _brushPrimary;
        private readonly Brush _brushDanger;
        private readonly Brush _brushTeal;
        private readonly Brush _brushWarning;

        public AntiCheatHeartbeatWindow()
        {
            InitializeComponent();
            // 从主题资源获取画笔，支持亮/暗主题切换
            _brushPrimary = TryFindResource("PrimaryBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
            _brushDanger = TryFindResource("DangerBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0xFB, 0x71, 0x85));
            _brushWarning = TryFindResource("WarningBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
            _brushTeal = TryFindResource("AccentTealBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0x2D, 0xD4, 0xBF));
            lbLogs.ItemsSource = _logEntries;

            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 6);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

                if (pulseRingOuter != null)
                    AnimationHelper.PulseRing(pulseRingOuter, 0.8, 1.5, 1.5);

                AddLog("🔄 正在建立安全连接...");
                AddLog($"📋 设备指纹: {GetShortFingerprint()}");

                var hb = GameLauncher.GetHeartbeatInfo();
                if (hb.IsActive)
                    AddLog($"💓 心跳已启动 → {hb.ServerIP}:{hb.Port}");
                else
                    AddLog("⏳ 等待心跳握手...");
            };
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 60) this.DragMove();
            };

            // 直接标记验证通过，心跳已在 ConnectAndHeartbeat 中启动
            btnVerify.Visibility = Visibility.Collapsed;
            countdownRing.Visibility = Visibility.Collapsed;
            txtCountdown.Visibility = Visibility.Collapsed;
            txtCountdown.Text = "✓";
            txtCountdown.Foreground = _brushPrimary;
            dotAntiCheat.Fill = _brushPrimary;
            dotIntegrity.Fill = _brushPrimary;
            dotNetwork.Fill = _brushTeal;
            btnClose.Visibility = Visibility.Visible;
            StartStatusUpdates();
        }

        private string GetShortFingerprint()
        {
            try
            {
                string fp = AntiCheatClient.GetMachineFingerprint();
                return fp.Length > 16 ? fp[..16] + "..." : fp;
            }
            catch { return "---"; }
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _logEntries.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                if (_logEntries.Count > 30)
                    _logEntries.RemoveAt(_logEntries.Count - 1);
            });
        }

        private int _lastLogPacketCount = -1;

        private void StartStatusUpdates()
        {
            _statusUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            _statusUpdateTimer.Tick += (s, e) =>
            {
                try
                {
                    bool acOk = App.AntiCheatPassed && !App.AntiCheatBlocked;
                    dotAntiCheat.Fill = acOk ? _brushPrimary : _brushDanger;

                    bool integrityOk = !App.AntiCheatBlocked;
                    dotIntegrity.Fill = integrityOk ? _brushPrimary : _brushDanger;

                    var hbInfo = GameLauncher.GetHeartbeatInfo();
                    bool networkOk = hbInfo.IsActive;
                    dotNetwork.Fill = networkOk ? _brushTeal : _brushWarning;

                    if (hbInfo.IsActive)
                    {
                        txtPacketInfo.Text = $"💓 心跳包: #{hbInfo.PacketCount} | 上次: {hbInfo.LastHeartbeatTime:HH:mm:ss}";
                        txtClientStatus.Text = $"服务器: {hbInfo.ServerIP}:{hbInfo.Port} | 状态: {(hbInfo.AntiCheatOk ? "✅ 正常" : "⚠️ 异常")}";
                        txtFingerprint.Text = $"设备: {hbInfo.ClientFingerprint?.Substring(0, 16)}...";

                        // 每次收到新心跳包时记录日志
                        if (hbInfo.PacketCount > _lastLogPacketCount)
                        {
                            _lastLogPacketCount = hbInfo.PacketCount;
                            AddLog($"💓 心跳 #{hbInfo.PacketCount} 已发送 → {hbInfo.ServerIP}:{hbInfo.Port}");
                        }
                    }
                    else
                    {
                        txtPacketInfo.Text = "⏳ 心跳包: 等待握手...";
                        txtClientStatus.Text = "状态: 连接中";
                    }
                }
                catch { /* 静默状态更新 */ }
            };
            _statusUpdateTimer.Start();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AddLog("🔚 关闭心跳窗口 | 后台心跳仍持续发送中");
            _statusUpdateTimer?.Stop();
            this.DialogResult = true;
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _statusUpdateTimer?.Stop();
            base.OnClosing(e);
        }
    }
}
