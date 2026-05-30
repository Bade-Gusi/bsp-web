using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BeiShuiCS2.Services;
using NAudio.Wave;

namespace BeiShuiCS2
{
    public partial class VoiceCallWindow : Window
    {
        private DispatcherTimer? _callTimer;
        private DispatcherTimer? _audioLevelUpdateTimer;  // UI refresh timer (not for capture)
        private DateTime _callStartTime;
        private bool _isMuted;
        private bool _isConnected;
        private readonly SignalRClient _signalR = new();
        private string _channelId = Guid.NewGuid().ToString("N")[..8];

        // NAudio capture
        private WaveInEvent? _waveIn;
        private float _currentAudioLevel;
        private readonly object _audioLevelLock = new();

        // 缓存画刷，避免定时器中反复 FindResource / new SolidColorBrush
        private readonly Brush _brushPrimary = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
        private readonly Brush _brushWarning = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
        private readonly Brush _brushMuted = new SolidColorBrush(Color.FromRgb(0x78, 0x8A, 0x80));
        private readonly Brush _brushDanger = new SolidColorBrush(Color.FromRgb(0xFB, 0x71, 0x85));

        // NAudio 设备列表
        public class AudioDeviceItem
        {
            public string Name { get; set; } = "";
            public int DeviceNumber { get; set; }
            public override string ToString() => Name;
        }

        public ObservableCollection<Participant> Participants { get; set; } = new();

        public VoiceCallWindow()
        {
            InitializeComponent();
            DataContext = this;

            // 初始化本地用户
            var localUser = new Participant
            {
                Name = "你",
                Initial = "你",
                IsHost = true,
                IsMuted = false,
                Status = "已连接",
                VolumePercent = 65,
                VolumeColor = _brushPrimary
            };
            Participants.Add(localUser);
            participantsList.ItemsSource = Participants;

            Loaded += async (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 12);
                this.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

                EnumerateAudioDevices();
                StartCallTimer();
                StartAudioCapture();
                StartAudioLevelUIRefresh();

                // 连接到 SignalR 信令服务器
                await ConnectToSignalR();
            };

            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 56) this.DragMove();
            };

            Closing += (s, e) =>
            {
                _callTimer?.Stop();
                _audioLevelUpdateTimer?.Stop();
                StopAudioCapture();
            };
        }

        #region Audio Devices

        private void EnumerateAudioDevices()
        {
            // 麦克风设备
            var micDevices = new List<AudioDeviceItem>
            {
                new AudioDeviceItem { Name = "默认麦克风", DeviceNumber = -1 }
            };
            try
            {
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var caps = WaveIn.GetCapabilities(i);
                    micDevices.Add(new AudioDeviceItem
                    {
                        Name = caps.ProductName,
                        DeviceNumber = i
                    });
                }
            }
            catch { /* 静默处理枚举异常 */ }

            cmbMicDevice.ItemsSource = micDevices;
            cmbMicDevice.SelectedIndex = 0;

            // 扬声器设备
            var speakerDevices = new List<AudioDeviceItem>
            {
                new AudioDeviceItem { Name = "默认扬声器", DeviceNumber = -1 }
            };
            try
            {
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var caps = WaveOut.GetCapabilities(i);
                    speakerDevices.Add(new AudioDeviceItem
                    {
                        Name = caps.ProductName,
                        DeviceNumber = i
                    });
                }
            }
            catch { /* 静默处理枚举异常 */ }

            cmbSpeakerDevice.ItemsSource = speakerDevices;
            cmbSpeakerDevice.SelectedIndex = 0;
        }

        #endregion

        #region Real Audio Capture (NAudio WaveIn)

        private void StartAudioCapture()
        {
            try
            {
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1), // 16kHz, 16-bit, mono
                    BufferMilliseconds = 50,
                    DeviceNumber = 0 // default device; updated via MicDevice_Changed
                };

                _waveIn.DataAvailable += OnAudioDataAvailable;
                _waveIn.RecordingStopped += (s, e) => { };
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceCall] NAudio 初始化失败: {ex.Message}");
            }
        }

        private void StopAudioCapture()
        {
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= OnAudioDataAvailable;
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }
            catch { /* 静默处理释放异常 */ }
        }

        private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            // 计算 RMS 音量电平 (0.0 ~ 1.0)
            float sum = 0;
            int sampleCount = e.BytesRecorded / 2; // 16-bit samples
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                float normalized = sample / 32768f;
                sum += normalized * normalized;
            }
            float rms = (float)Math.Sqrt(sum / Math.Max(sampleCount, 1));

            lock (_audioLevelLock)
            {
                // Smooth with exponential moving average to avoid jitter
                _currentAudioLevel = (_currentAudioLevel * 0.6f) + (rms * 0.4f);
            }
        }

        private void RestartAudioCapture(int deviceNumber)
        {
            StopAudioCapture();
            try
            {
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1),
                    BufferMilliseconds = 50,
                    DeviceNumber = deviceNumber
                };
                _waveIn.DataAvailable += OnAudioDataAvailable;
                _waveIn.RecordingStopped += (s, e) => { };
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VoiceCall] 切换麦克风失败: {ex.Message}");
            }
        }

        #endregion

        #region UI Timers

        private void StartCallTimer()
        {
            _callStartTime = DateTime.Now;
            _callTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _callTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _callStartTime;
                txtCallStatus.Text = _isMuted
                    ? $"已静音 · {elapsed:mm\\:ss}"
                    : $"通话中 · {elapsed:mm\\:ss}";

                if (_callTimer.IsEnabled)
                {
                    dotCallStatus.Fill = _isMuted ? _brushWarning : _brushPrimary;
                }
                else
                {
                    dotCallStatus.Fill = _brushDanger;
                    txtCallStatus.Text = "已断开";
                }
            };
            _callTimer.Start();
        }

        private void StartAudioLevelUIRefresh()
        {
            _audioLevelUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _audioLevelUpdateTimer.Tick += (s, e) =>
            {
                // --- 更新本地用户音量 ---
                var me = Participants.FirstOrDefault(p => p.IsHost);
                if (me != null)
                {
                    if (me.IsMuted)
                    {
                        me.VolumePercent = 0;
                        me.VolumeColor = _brushMuted;
                    }
                    else
                    {
                        float level;
                        lock (_audioLevelLock)
                        {
                            level = _currentAudioLevel;
                        }

                        // Map audio level (0.0~1.0) to VolumePercent (5~100)
                        // Use a logarithmic feel: square root to exaggerate low levels
                        double mapped = Math.Sqrt(level) * 100.0;
                        int newVol = Math.Clamp((int)(mapped * 1.5), 5, 100);
                        me.VolumePercent = newVol;
                        me.VolumeColor = newVol > 50
                            ? _brushPrimary
                            : newVol > 20
                                ? _brushWarning
                                : _brushMuted;

                        // Update IsSpeaking based on whether audio level is meaningful
                        me.IsSpeaking = level > 0.015f;
                    }
                }
            };
            _audioLevelUpdateTimer.Start();
        }

        #endregion

        #region SignalR

        private async Task ConnectToSignalR()
        {
            _signalR.OnConnectionChanged += (connected) =>
            {
                _isConnected = connected;
                if (connected)
                {
                    _ = _signalR.JoinChannelAsync(_channelId);
                }

                // Update local user status to reflect real connection state
                var me = Participants.FirstOrDefault(p => p.IsHost);
                if (me != null)
                {
                    me.Status = connected ? "已连接" : "已断开";
                }
            };

            _signalR.OnUserJoined += (userId) =>
            {
                // Other users join the call
                if (!Participants.Any(p => p.SteamId == userId))
                {
                    Participants.Add(new Participant
                    {
                        Name = $"用户 {userId[..4]}",
                        Initial = "U",
                        SteamId = userId,
                        Status = "已连接",
                        VolumePercent = 30,
                        VolumeColor = _brushMuted
                    });
                }
            };

            _signalR.OnUserLeft += (userId) =>
            {
                var p = Participants.FirstOrDefault(x => x.SteamId == userId);
                if (p != null) Participants.Remove(p);
            };

            await _signalR.ConnectAsync();
        }

        #endregion

        #region Control Events

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            _isMuted = !_isMuted;
            var me = Participants.FirstOrDefault(p => p.IsHost);
            if (me != null)
            {
                me.IsMuted = _isMuted;
                me.VolumePercent = _isMuted ? 0 : 65;
                me.Status = _isMuted ? "静音中" : "已连接";
            }

            // 按钮动效
            AnimationHelper.ButtonPressPulse(btnMute);

            if (_isMuted)
            {
                btnMute.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0x71, 0x85));
                btnMute.Content = "\uEC35"; // 麦克风关闭图标
            }
            else
            {
                btnMute.Foreground = (Brush)FindResource("TextSecondaryBrush");
                btnMute.Content = "\uEC34"; // 麦克风图标
            }
        }

        private void ShareScreen_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.ButtonPressPulse(btnShareScreen);

            // 打开屏幕分享窗口
            var screenShareWin = new ScreenShareWindow { Owner = this };
            screenShareWin.Show();
        }

        private async void EndCall_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.ButtonPressPulse(btnEndCall);

            btnEndCall.IsEnabled = false;
            _callTimer?.Stop();
            _audioLevelUpdateTimer?.Stop();
            StopAudioCapture();

            // 断开 SignalR
            await _signalR.LeaveChannelAsync(_channelId);
            await _signalR.DisconnectAsync();

            txtCallStatus.Text = "通话已结束";
            dotCallStatus.Fill = _brushDanger;

            // 退出动效
            var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            closeTimer.Tick += (_, _) =>
            {
                closeTimer.Stop();
                AnimationHelper.WindowExit(this, () => this.Close());
            };
            closeTimer.Start();
        }

        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 实际项目中调整 NAudio 音量
            var me = Participants.FirstOrDefault(p => p.IsHost);
            if (me != null && !_isMuted)
            {
                me.VolumePercent = (int)e.NewValue;
            }
        }

        private void MicDevice_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMicDevice.SelectedItem is AudioDeviceItem item && item.DeviceNumber >= 0)
            {
                RestartAudioCapture(item.DeviceNumber);
            }
        }

        private void SpeakerDevice_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSpeakerDevice.SelectedItem is AudioDeviceItem item && item.DeviceNumber >= 0)
            {
                // 实际项目中切换到所选扬声器设备
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => Close());
        }

        #endregion
    }

    public class Participant : INotifyPropertyChanged
    {
        private string _name = "";
        private string _initial = "";
        private string _steamId = "";
        private bool _isHost;
        private bool _isMuted;
        private bool _isSpeaking;
        private string _status = "已连接";
        private int _volumePercent = 50;
        private Brush _volumeColor = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string Initial { get => _initial; set { _initial = value; OnPropertyChanged(); } }
        public string SteamId { get => _steamId; set { _steamId = value; OnPropertyChanged(); } }
        public bool IsHost { get => _isHost; set { _isHost = value; OnPropertyChanged(); } }
        public bool IsMuted { get => _isMuted; set { _isMuted = value; OnPropertyChanged(); OnPropertyChanged(nameof(Status)); } }
        public bool IsSpeaking { get => _isSpeaking; set { _isSpeaking = value; OnPropertyChanged(); } }
        public string Status
        {
            get => IsMuted ? "静音中" : _status;
            set { _status = value; OnPropertyChanged(); }
        }
        public int VolumePercent { get => _volumePercent; set { _volumePercent = value; OnPropertyChanged(); } }
        public Brush VolumeColor { get => _volumeColor; set { _volumeColor = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
