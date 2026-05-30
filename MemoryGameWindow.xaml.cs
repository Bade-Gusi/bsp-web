using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class MemoryGameWindow : Window
    {
        private int[] cards = null!;
        private bool[] revealed = null!;
        private int firstCard = -1;
        private int secondCard = -1;
        private int score = 0;
        private int timeLeft = 60;
        private bool isProcessing = false;
        private DispatcherTimer timer = null!;

        public MemoryGameWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            StartGame();
        }

        private void StartGame()
        {
            cards = new int[16];
            revealed = new bool[16];
            score = 0;
            timeLeft = 60;
            firstCard = -1;
            secondCard = -1;
            isProcessing = false;

            Random rand = new Random();
            for (int i = 0; i < 8; i++)
            {
                cards[i] = i + 1;
                cards[i + 8] = i + 1;
            }
            for (int i = 15; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                int temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }

            gridGame.Children.Clear();
            for (int i = 0; i < 16; i++)
            {
                var card = CreateCard(i);
                gridGame.Children.Add(card);
            }

            UpdateUI();
            StartTimer();
        }

        private Border CreateCard(int index)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F")),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(6),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = index,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };

            card.MouseLeftButtonDown += Card_Click;

            var text = new TextBlock
            {
                Text = "?",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            card.Child = text;

            // 进入动画
            var delay = index * 30;
            var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
            {
                BeginTime = TimeSpan.FromMilliseconds(delay)
            };
            var scaleAnim = new DoubleAnimation(0.8, 1, TimeSpan.FromSeconds(0.4))
            {
                BeginTime = TimeSpan.FromMilliseconds(delay),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            card.BeginAnimation(OpacityProperty, opacityAnim);
            card.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            card.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            return card;
        }

        private void Card_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isProcessing) return;

            var card = sender as Border;
            if (card == null) return;
            int index = (int)card.Tag;

            if (revealed[index]) return;
            if (firstCard == index) return;

            RevealCard(card, index);

            if (firstCard == -1)
            {
                firstCard = index;
            }
            else
            {
                secondCard = index;
                CheckMatch();
            }
        }

        private void RevealCard(Border card, int index)
        {
            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            var text = card.Child as TextBlock;
            if (text != null)
            {
                text.Text = cards[index].ToString();
                text.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void HideCard(Border card)
        {
            card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2E1F"));
            var text = card.Child as TextBlock;
            if (text != null)
            {
                text.Text = "?";
                text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }
        }

        private void CheckMatch()
        {
            isProcessing = true;

            if (cards[firstCard] == cards[secondCard])
            {
                revealed[firstCard] = true;
                revealed[secondCard] = true;
                score += 10;
                firstCard = -1;
                secondCard = -1;
                isProcessing = false;
                UpdateUI();

                // 检查是否全部完成
                bool allRevealed = true;
                for (int i = 0; i < 16; i++)
                {
                    if (!revealed[i])
                    {
                        allRevealed = false;
                        break;
                    }
                }
                if (allRevealed)
                {
                    timer.Stop();
                    MessageBox.Show($"恭喜! 得分: {score}", "游戏结束");
                }
            }
            else
            {
                var firstBorder = gridGame.Children[firstCard] as Border;
                var secondBorder = gridGame.Children[secondCard] as Border;

                var delay = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.8) };
                delay.Tick += (s, e) =>
                {
                    delay.Stop();
                    if (firstBorder != null) HideCard(firstBorder);
                    if (secondBorder != null) HideCard(secondBorder);
                    firstCard = -1;
                    secondCard = -1;
                    isProcessing = false;
                };
                delay.Start();
            }
        }

        private void StartTimer()
        {
            if (timer != null) timer.Stop();
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) =>
            {
                timeLeft--;
                UpdateUI();
                if (timeLeft <= 0)
                {
                    timer.Stop();
                    MessageBox.Show($"时间到! 得分: {score}", "游戏结束");
                    StartGame();
                }
            };
            timer.Start();
        }

        private void UpdateUI()
        {
            txtScore.Text = $"得分: {score}";
            txtTime.Text = $"时间: {timeLeft}";
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null) timer.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
