using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BeiShuiCS2.Services;

namespace BeiShuiCS2
{
    public partial class ScreenShareWindow : Window
    {
        private DispatcherTimer? _streamTimer;
        private DateTime _streamStartTime;
        private bool _isSharing = true;
        private bool _isPaused;
        private int _currentFps = 30;
        private readonly SignalRClient _signalR = new();
        private readonly string _channelId = "screen-" + Guid.NewGuid().ToString("N")[..8];
        private CancellationTokenSource? _captureCts;
        private Task? _captureTask;

        public ObservableCollection<ViewerInfo> Viewers { get; set; } = new();

        public ScreenShareWindow()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += async (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 4);
                this.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

                cmbQuality.SelectionChanged += Quality_Changed;

                txtFps.Text = _currentFps.ToString();
                txtStreamInfo.Text = "编码: H.264 · 分辨率: 1280×720";
                txtStreamStatus.Text = $"正在分享 · {_currentFps} FPS";

                StartStreamTimer();
                await ConnectToSignalR();

                // 启动真实屏幕捕获
                StartCapture();
            };

            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 56) this.DragMove();
            };

            Closing += (s, e) =>
            {
                cmbQuality.SelectionChanged -= Quality_Changed;
                _streamTimer?.Stop();
                StopCapture();
            };
        }

        #region 真实屏幕捕获

        private void StartCapture()
        {
            _captureCts = new CancellationTokenSource();
            _captureTask = Task.Run(() => CaptureLoop(_captureCts.Token));
        }

        private void StopCapture()
        {
            _captureCts?.Cancel();
            _captureTask?.Wait(500);
            _captureCts?.Dispose();
            _captureCts = null;
        }

        private void CaptureLoop(CancellationToken ct)
        {
            int frameInterval = 1000 / Math.Max(_currentFps, 1);
            var dpiScale = ScreenAdapter.GetDpiScale(this);
            if (dpiScale <= 0) dpiScale = 1.0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_isPaused || !_isSharing)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    // 窗口未激活时降低帧率
                    if (!this.IsActive)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    var startTime = DateTime.UtcNow;
                    var (width, height) = GetCaptureResolution();

                    using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bitmap.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                    }

                    // 缩放预览
                    using var preview = ScaleBitmap(bitmap, 320, 200);
                    var imageSource = BitmapToImageSource(preview);

                    // 更新 UI
                    Dispatcher.Invoke(() =>
                    {
                        previewImage.Source = imageSource;
                    });

                    // 控制在目标帧率
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    var delay = (int)(frameInterval - elapsed);
                    if (delay > 0)
                        Thread.Sleep(delay);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScreenShare] 捕获异常: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        private (int width, int height) GetCaptureResolution()
        {
            return cmbQuality.SelectedItem is ComboBoxItem item && item.Tag is string tag
                ? tag switch
                {
                    "high" => (1920, 1080),
                    "medium" => (1280, 720),
                    "low" => (854, 480),
                    _ => (1280, 720)
                }
                : (1280, 720);
        }

        private static System.Drawing.Bitmap ScaleBitmap(System.Drawing.Bitmap source, int maxWidth, int maxHeight)
        {
            double ratio = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);
            int newW = (int)(source.Width * ratio);
            int newH = (int)(source.Height * ratio);
            if (newW <= 0) newW = 1;
            if (newH <= 0) newH = 1;

            var scaled = new System.Drawing.Bitmap(newW, newH);
            using (var g = System.Drawing.Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, newW, newH);
            }
            return scaled;
        }

        private static ImageSource BitmapToImageSource(System.Drawing.Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }

        #endregion

        #region Timers

        private void StartStreamTimer()
        {
            _streamStartTime = DateTime.Now;

            _streamTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _streamTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _streamStartTime;
                txtUptime.Text = _isPaused
                    ? $"已暂停 · {elapsed:mm\\:ss}"
                    : $"已分享 {elapsed:mm\\:ss}";

                txtFps.Text = _currentFps.ToString();
                txtStreamStatus.Text = _isPaused
                    ? "分享已暂停"
                    : $"正在分享 · {_currentFps} FPS";

                txtStreamInfo.Text = $"分辨率: {GetResolutionString()} · 目标 {_currentFps} FPS";

                txtViewerCount.Text = $" · {Viewers.Count} 人正在观看";

                dotStreamStatus.Fill = _isPaused
                    ? (Brush)FindResource("WarningBrush")
                    : _isSharing
                        ? (Brush)FindResource("PrimaryBrush")
                        : (Brush)FindResource("DangerBrush");
            };
            _streamTimer.Start();
        }

        private string GetResolutionString()
        {
            if (cmbQuality?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                return tag switch
                {
                    "high" => "1920×1080",
                    "medium" => "1280×720",
                    "low" => "854×480",
                    _ => "1280×720"
                };
            }
            return "1280×720";
        }

        #endregion

        #region Control Events

        private void ToggleShare_Click(object sender, RoutedEventArgs e)
        {
            _isSharing = !_isSharing;

            if (_isSharing)
            {
                btnToggleShare.Content = "停止分享";
                btnToggleShare.Style = (Style)FindResource("BtnDanger");
                txtStreamStatus.Text = $"正在分享 · {_currentFps} FPS";
                _ = _signalR.StartScreenShareAsync(_channelId);
            }
            else
            {
                btnToggleShare.Content = "开始分享";
                btnToggleShare.Style = (Style)FindResource("BtnPrimary");
                txtStreamStatus.Text = "分享已停止";
                dotStreamStatus.Fill = (Brush)FindResource("DangerBrush");
                _ = _signalR.StopScreenShareAsync(_channelId);
            }
        }

        private void PauseShare_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                btnPauseShare.Content = "继续分享";
                txtStreamStatus.Text = "分享已暂停";
                dotStreamStatus.Fill = (Brush)FindResource("WarningBrush");
            }
            else
            {
                btnPauseShare.Content = "暂停分享";
                txtStreamStatus.Text = $"正在分享 · {_currentFps} FPS";
                dotStreamStatus.Fill = (Brush)FindResource("PrimaryBrush");
            }
        }

        private void Quality_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbQuality.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
                return;

            _currentFps = tag switch
            {
                "high" => 60,
                "medium" => 30,
                "low" => 15,
                _ => 30
            };

            txtFps.Text = _currentFps.ToString();
            txtStreamInfo.Text = $"分辨率: {GetResolutionString()} · 目标 {_currentFps} FPS";
        }

        private void Invite_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_channelId);
            txtStreamStatus.Text = "邀请链接已复制到剪贴板";
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            _streamTimer?.Stop();
            await _signalR.StopScreenShareAsync(_channelId);
            await _signalR.LeaveChannelAsync(_channelId);
            await _signalR.DisconnectAsync();
            AnimationHelper.WindowExit(this, () => Close());
        }

        private async Task ConnectToSignalR()
        {
            _signalR.OnConnectionChanged += (connected) =>
            {
                if (connected)
                    _ = _signalR.JoinChannelAsync(_channelId);
            };

            _signalR.OnUserJoined += (userId) =>
            {
                if (!Viewers.Any(v => v.Name == userId))
                {
                    Viewers.Add(new ViewerInfo
                    {
                        Name = userId,
                        DotColor = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
                    });
                }
            };

            _signalR.OnUserLeft += (userId) =>
            {
                var viewer = Viewers.FirstOrDefault(v => v.Name == userId);
                if (viewer != null)
                    Viewers.Remove(viewer);
            };

            await _signalR.ConnectAsync();
        }

        #endregion
    }

    public class ViewerInfo : INotifyPropertyChanged
    {
        private string _name = "";
        private Brush _dotColor = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public Brush DotColor { get => _dotColor; set { _dotColor = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
