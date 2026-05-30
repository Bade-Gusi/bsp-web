using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class AddFriendWindow : Window
    {
        private int _foundUserId;

        public AddFriendWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            };
            txtFriendId.GotFocus += TxtFriendId_GotFocus;
            txtFriendId.LostFocus += TxtFriendId_LostFocus;
            txtFriendId.TextChanged += TxtFriendId_TextChanged;
        }

        private void TxtFriendId_GotFocus(object sender, RoutedEventArgs e)
        {
            lblPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void TxtFriendId_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFriendId.Text))
                lblPlaceholder.Visibility = Visibility.Visible;
        }

        private void TxtFriendId_TextChanged(object sender, TextChangedEventArgs e)
        {
            lblPlaceholder.Visibility = string.IsNullOrEmpty(txtFriendId.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            var query = txtFriendId.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("请输入用户名或Steam ID", "提示");
                return;
            }

            btnSearch.IsEnabled = false;
            btnSearch.Content = "搜索中...";

            var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>($"/api/users/search?q={Uri.EscapeDataString(query)}");

            btnSearch.IsEnabled = true;
            btnSearch.Content = "搜索";

            if (result.Success && result.Data != null && result.Data.Length > 0)
            {
                var user = result.Data[0];
                _foundUserId = user.GetProperty("id").GetInt32();
                txtResultName.Text = user.GetProperty("username").GetString() ?? "";
                txtResultInitial.Text = (txtResultName.Text.Length > 0 ? txtResultName.Text[..1] : "?").ToUpper();
                var mmrVal = user.TryGetProperty("mmr", out var mmr) ? mmr.GetInt32() : 0;
                txtResultId.Text = $"MMR: {mmrVal}";

                resultPanel.Visibility = Visibility.Visible;
                resultPanel.Opacity = 0;
                resultPanel.RenderTransform = new TranslateTransform(0, 20);

                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                var translateAnim = new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.4))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

                resultPanel.BeginAnimation(OpacityProperty, opacityAnim);
                resultPanel.RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
            }
            else
            {
                MessageBox.Show("未找到该用户", "提示");
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            if (_foundUserId <= 0) return;

            var result = await ApiClient.PostAsync<object>($"/api/friends/request/{_foundUserId}", new { });
            if (result.Success)
            {
                MessageBox.Show($"已向 {txtResultName.Text} 发送好友请求", "提示");
                AnimationHelper.WindowExit(this, () => this.Close());
            }
            else
            {
                MessageBox.Show(result.Error ?? "添加好友失败", "提示");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }
}
