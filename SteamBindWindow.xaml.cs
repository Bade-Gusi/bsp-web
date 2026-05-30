using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class SteamBindWindow : Window
    {
        public string SteamId { get; set; } = "";

        public SteamBindWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35)));
                txtSteamId.Text = string.IsNullOrEmpty(SteamId) ? "" : $"Steam ID: {SteamId}";
            };
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 56) DragMove();
            };
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            // 关闭当前窗口，打开注册页（已带 SteamId）
            var register = new RegisterWindow
            {
                Owner = this.Owner ?? Application.Current.MainWindow,
                PrefillSteamId = SteamId
            };
            register.Closed += (_, _) => this.Close();
            register.Show();
            this.Hide();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // 回到登录页
            var owner = this.Owner;
            this.Close();
            if (owner is Window win && win.IsVisible == false)
                win.Show();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => Close());
        }
    }
}
