using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class ReactionGameWindow : Window
    {
        private Random random = new Random();
        private DispatcherTimer waitTimer = null!;
        private DateTime startTime;
        private bool isWaiting = false;
        private bool isClickable = false;
        private int bestTime = int.MaxValue;

        public ReactionGameWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            StartNewRound();
        }

        private void StartNewRound()
        {
            isWaiting = false;
            isClickable = false;
            txtResult.Visibility = Visibility.Collapsed;
            txtInstruction.Text = "点击区域开始...";
            txtInstruction.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            reactionArea.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F"));
        }

        private void ReactionArea_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!isWaiting)
            {
                // 开始等待
                isWaiting = true;
                isClickable = false;
                txtInstruction.Text = "等待绿色...";
                txtInstruction.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                // 随机等待时间 2-5秒
                int waitMs = random.Next(2000, 5001);

                waitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(waitMs) };
                waitTimer.Tick += (s, args) =>
                {
                    waitTimer.Stop();
                    if (isWaiting)
                    {
                        // 显示绿色
                        ShowGreen();
                    }
                };
                waitTimer.Start();
            }
            else if (!isClickable)
            {
                // 太早点击 - 红色
                waitTimer?.Stop();
                txtInstruction.Text = "太早了! 点击重新开始";
                txtInstruction.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                reactionArea.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                isWaiting = false;
            }
            else
            {
                // 记录反应时间
                var reactionTime = (DateTime.Now - startTime).TotalMilliseconds;
                ShowResult((int)reactionTime);
            }
        }

        private void ShowGreen()
        {
            isClickable = true;
            startTime = DateTime.Now;
            txtInstruction.Text = "点击!";
            txtInstruction.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            reactionArea.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));

            // 如果5秒内没点击，也结束
            var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timeout.Tick += (s, e) =>
            {
                timeout.Stop();
                if (isClickable)
                {
                    ShowResult(5000);
                }
            };
            timeout.Start();
        }

        private void ShowResult(int ms)
        {
            isClickable = false;
            txtResult.Text = $"{ms} ms";
            txtResult.Visibility = Visibility.Visible;
            txtInstruction.Text = "点击继续下一轮";

            // 根据速度显示颜色
            if (ms < 200)
            {
                txtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // 金色
            }
            else if (ms < 300)
            {
                txtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")); // 绿色
            }
            else if (ms < 400)
            {
                txtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22D3EE")); // 青色
            }
            else
            {
                txtResult.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")); // 灰色
            }

            // 更新最佳成绩
            if (ms < bestTime)
            {
                bestTime = ms;
                txtBest.Text = $"最佳: {ms} ms";
            }

            // 更新历史记录
            UpdateHistory(ms);

            reactionArea.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F"));
            isWaiting = false;
        }

        private void UpdateHistory(int ms)
        {
            // 简单显示最近一次
            historyList.Items.Add(new TextBlock
            {
                Text = $"{ms}ms",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                Margin = new Thickness(0, 0, 12, 0),
                FontSize = 14,
                FontWeight = FontWeights.Bold
            });
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            waitTimer?.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
