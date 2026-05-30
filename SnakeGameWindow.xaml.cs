using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class SnakeGameWindow : Window
    {
        private const int GRID_SIZE = 20;
        private const int CELL_SIZE = 20;

        private Snake snake = null!;
        private Point food;
        private Random random = new Random();
        private DispatcherTimer gameTimer = null!;
        private bool isPaused = false;
        private bool isGameOver = false;
        private int bestScore = 0;

        public SnakeGameWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            InitializeGame();
        }

        private void InitializeGame()
        {
            gameCanvas.Width = GRID_SIZE * CELL_SIZE;
            gameCanvas.Height = GRID_SIZE * CELL_SIZE;

            snake = new Snake(GRID_SIZE / 2, GRID_SIZE / 2);
            SpawnFood();

            gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            gameTimer.Tick += GameTick;
            gameTimer.Start();

            Draw();
        }

        private void SpawnFood()
        {
            int x, y;
            do
            {
                x = random.Next(0, GRID_SIZE);
                y = random.Next(0, GRID_SIZE);
            } while (snake.Contains(x, y));

            food = new Point(x, y);
        }

        private void GameTick(object? sender, EventArgs e)
        {
            if (isPaused || isGameOver) return;

            snake.Move();

            // 检查碰撞
            if (snake.Head.X < 0 || snake.Head.X >= GRID_SIZE ||
                snake.Head.Y < 0 || snake.Head.Y >= GRID_SIZE ||
                snake.CheckSelfCollision())
            {
                GameOver();
                return;
            }

            // 检查是否吃到食物
            if ((int)snake.Head.X == (int)food.X && (int)snake.Head.Y == (int)food.Y)
            {
                snake.Grow();
                SpawnFood();
                txtScore.Text = snake.Length.ToString();

                // 增加速度
                if (snake.Length % 5 == 0)
                {
                    gameTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(50, gameTimer.Interval.TotalMilliseconds - 10));
                    txtLevel.Text = (int.Parse(txtLevel.Text ?? "0") + 1).ToString();
                }
            }

            Draw();
        }

        private void Draw()
        {
            gameCanvas.Children.Clear();

            // 绘制食物
            var foodRect = new Rectangle
            {
                Width = CELL_SIZE - 2,
                Height = CELL_SIZE - 2,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(foodRect, food.X * CELL_SIZE + 1);
            Canvas.SetTop(foodRect, food.Y * CELL_SIZE + 1);
            gameCanvas.Children.Add(foodRect);

            // 绘制蛇
            for (int i = 0; i < snake.Length; i++)
            {
                var segment = snake[i];
                var isHead = i == 0;

                var rect = new Rectangle
                {
                    Width = CELL_SIZE - 2,
                    Height = CELL_SIZE - 2,
                    Fill = isHead
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
                    RadiusX = isHead ? 6 : 4,
                    RadiusY = isHead ? 6 : 4
                };

                Canvas.SetLeft(rect, segment.X * CELL_SIZE + 1);
                Canvas.SetTop(rect, segment.Y * CELL_SIZE + 1);
                gameCanvas.Children.Add(rect);
            }
        }

        private void GameOver()
        {
            isGameOver = true;
            gameTimer.Stop();

            int score = snake.Length;
            if (score > bestScore)
            {
                bestScore = score;
                txtBest.Text = bestScore.ToString();
            }

            MessageBox.Show($"游戏结束!\n得分: {score}", "贪吃蛇", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isGameOver) return;

            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    snake.Direction = Direction.Up;
                    break;
                case Key.Down:
                case Key.S:
                    snake.Direction = Direction.Down;
                    break;
                case Key.Left:
                case Key.A:
                    snake.Direction = Direction.Left;
                    break;
                case Key.Right:
                case Key.D:
                    snake.Direction = Direction.Right;
                    break;
                case Key.Space:
                    BtnPause_Click(this, new RoutedEventArgs());
                    break;
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (isGameOver) return;

            isPaused = !isPaused;
            btnPause.Content = isPaused ? "继续" : "暂停";

            if (isPaused)
            {
                gameTimer.Stop();
            }
            else
            {
                gameTimer.Start();
            }
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            isGameOver = false;
            isPaused = false;
            btnPause.Content = "暂停";
            txtLevel.Text = "1";
            txtScore.Text = "0";

            snake = new Snake(GRID_SIZE / 2, GRID_SIZE / 2);
            SpawnFood();
            gameTimer.Interval = TimeSpan.FromMilliseconds(150);
            gameTimer.Start();

            Draw();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            gameTimer?.Stop();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    public enum Direction { Up, Down, Left, Right }

    public class Snake
    {
        private List<Point> segments = new List<Point>();

        public Point Head => segments[0];
        public int Length => segments.Count;
        public Direction Direction { get; set; } = Direction.Right;
        private Direction lastDirection = Direction.Right;

        public Snake(int x, int y)
        {
            segments.Add(new Point(x, y));
            segments.Add(new Point(x - 1, y));
            segments.Add(new Point(x - 2, y));
        }

        public Point this[int index] => segments[index];

        public bool Contains(int x, int y)
        {
            return segments.Any(s => (int)s.X == x && (int)s.Y == y);
        }

        public void Move()
        {
            lastDirection = Direction;

            var newHead = new Point(Head.X, Head.Y);

            switch (Direction)
            {
                case Direction.Up: newHead.Y--; break;
                case Direction.Down: newHead.Y++; break;
                case Direction.Left: newHead.X--; break;
                case Direction.Right: newHead.X++; break;
            }

            segments.Insert(0, newHead);
            segments.RemoveAt(segments.Count - 1);
        }

        public void Grow()
        {
            segments.Add(segments[segments.Count - 1]);
        }

        public bool CheckSelfCollision()
        {
            return segments.Skip(1).Any(s => (int)s.X == (int)Head.X && (int)s.Y == (int)Head.Y);
        }
    }
}
