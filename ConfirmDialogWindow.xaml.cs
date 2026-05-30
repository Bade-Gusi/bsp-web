using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class ConfirmDialogWindow : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmDialogWindow(string title, string message, string confirmText = "确认删除")
        {
            InitializeComponent();
            txtTitle.Text = title;
            txtMessage.Text = message;
            btnConfirm.Content = confirmText;

            // 标题栏拖拽
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 52) this.DragMove();
            };

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 窗口淡入
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.25)));

            // 内容缩放淡入
            var root = this.Content as Grid;
            if (root != null)
            {
                root.Opacity = 0;
                root.RenderTransformOrigin = new Point(0.5, 0.5);
                root.RenderTransform = new ScaleTransform(0.92, 0.92);

                root.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
                root.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(0.35))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
                root.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(0.92, 1, TimeSpan.FromSeconds(0.35))
                    { EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut } });
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            // 设置 DialogResult=true → ShowDialog() 返回 true，删除代码才会执行
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.12));
            fadeOut.Completed += (_, _) => DialogResult = true;
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            // 设置 DialogResult=false → ShowDialog() 返回 false
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.12));
            fadeOut.Completed += (_, _) => DialogResult = false;
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
