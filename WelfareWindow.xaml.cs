using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BeiShuiCS2.Services;

namespace BeiShuiCS2
{
    public partial class WelfareWindow : Window
    {
        public ObservableCollection<WelfareItemVM> WelfareItems { get; set; } = new();

        public WelfareWindow()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += async (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 10);

                // 入场动画
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
                this.BeginAnimation(OpacityProperty, fadeIn);

                // 从 API 加载公益项目
                var items = await HolidayService.GetWelfareItemsAsync();
                foreach (var item in items)
                {
                    WelfareItems.Add(new WelfareItemVM
                    {
                        Title = item.Title,
                        Summary = item.Summary,
                        Link = item.Link,
                        Icon = item.Icon,
                        Color1Obj = (Color)ColorConverter.ConvertFromString(item.Color1),
                        Color2Obj = (Color)ColorConverter.ConvertFromString(item.Color2)
                    });
                }
                welfareList.ItemsSource = WelfareItems;

                if (items.Count > 0)
                    await LoadWebContent(items[0].Link);
            };

            MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 56) DragMove();
            };
        }

        private async System.Threading.Tasks.Task LoadWebContent(string url)
        {
            txtLoading.Visibility = Visibility.Collapsed;
            try
            {
                await webContent.EnsureCoreWebView2Async();
                webContent.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                webContent.CoreWebView2.Settings.IsScriptEnabled = true;
                webContent.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                txtLoading.Text = $"加载失败: {ex.Message}";
                txtLoading.Visibility = Visibility.Visible;
            }
        }

        private void WelfareItem_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag is string url && !string.IsNullOrEmpty(url))
            {
                _ = LoadWebContent(url);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.25));
            fadeOut.Completed += (_, _) => Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    public class WelfareItemVM
    {
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Link { get; set; } = "";
        public string Icon { get; set; } = "❤️";
        public Color Color1Obj { get; set; }
        public Color Color2Obj { get; set; }
    }
}
