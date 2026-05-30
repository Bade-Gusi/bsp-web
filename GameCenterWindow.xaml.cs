using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class GameCenterWindow : Window
    {
        public ObservableCollection<GameItem> Games { get; set; } = new();

        public GameCenterWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += (s, e) =>
            {
                AnimationHelper.CreateFloatingParticles(particleCanvas, 14);
                AnimationHelper.StaggerEntrance(contentRoot.Children);
                LoadGames();

                Dispatcher.BeginInvoke(() =>
                {
                    for (int i = 0; i < Games.Count; i++)
                    {
                        var container = gameList.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                        if (container != null)
                        {
                            int idx = i;
                            container.Opacity = 0;
                            var tt = new TranslateTransform(0, 20);
                            container.RenderTransform = tt;

                            container.BeginAnimation(UIElement.OpacityProperty,
                                new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.35))
                                {
                                    BeginTime = TimeSpan.FromSeconds(0.07 * idx),
                                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                                });

                            tt.BeginAnimation(TranslateTransform.YProperty,
                                new DoubleAnimation(20, 0, TimeSpan.FromSeconds(0.4))
                                {
                                    BeginTime = TimeSpan.FromSeconds(0.07 * idx),
                                    EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut }
                                });
                        }
                    }
                }, DispatcherPriority.Loaded);
            };
        }

        private void LoadGames()
        {
            Games.Add(new GameItem { Name = "记忆翻牌", Icon = "🧠", Desc = "考验你的记忆力", Type = "memory" });
            Games.Add(new GameItem { Name = "反应测试", Icon = "⚡", Desc = "测试你的反应速度", Type = "reaction" });
            Games.Add(new GameItem { Name = "贪吃蛇", Icon = "🐍", Desc = "经典街机小游戏", Type = "snake" });
            Games.Add(new GameItem { Name = "井字棋", Icon = "⭕", Desc = "与AI对战", Type = "tictactoe" });
            Games.Add(new GameItem { Name = "猜数字", Icon = "🔢", Desc = "猜中电脑的随机数", Type = "guess" });

            gameList.ItemsSource = Games;
        }

        private void GameCard_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement btn) || btn.Tag == null) return;
            string type = btn.Tag.ToString()!;

            Window? gameWindow = type switch
            {
                "memory" => new MemoryGameWindow { Owner = this },
                "reaction" => new ReactionGameWindow { Owner = this },
                "snake" => new SnakeGameWindow { Owner = this },
                "tictactoe" => new TicTacToeWindow { Owner = this },
                _ => null
            };

            if (gameWindow != null)
            {
                var clickAnim = new DoubleAnimation(0.95, 1, TimeSpan.FromSeconds(0.15));
                clickAnim.EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut };
                btn.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.7, TimeSpan.FromSeconds(0.05)));
                btn.BeginAnimation(OpacityProperty, clickAnim);

                gameWindow.ShowDialog();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => this.Close());
        }
    }

    public class GameItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Desc { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
