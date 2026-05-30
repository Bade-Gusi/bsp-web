using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class StatsOverviewWindow : Window
    {
        public StatsOverviewWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                AnimateCards();
            };
        }

        private void AnimateCards()
        {
            var content = this.Content as FrameworkElement;
            if (content == null) return;

            int i = 0;
            foreach (var child in FindVisualChildren<Border>(content))
            {
                if (child.Style == null || child.Name == "") continue;
                child.Opacity = 0;
                child.RenderTransform = new System.Windows.Media.TranslateTransform(0, 20);

                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                {
                    BeginTime = TimeSpan.FromSeconds(0.1 * i),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnim = new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.5))
                {
                    BeginTime = TimeSpan.FromSeconds(0.1 * i),
                    EasingFunction = new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut }
                };

                child.BeginAnimation(OpacityProperty, opacityAnim);
                child.RenderTransform.BeginAnimation(
                    System.Windows.Media.TranslateTransform.YProperty, translateAnim);
                i++;
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild) yield return tChild;
                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
