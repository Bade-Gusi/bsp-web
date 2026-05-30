using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BeiShuiCS2
{
    public partial class TicTacToeWindow : Window
    {
        private char[] board = new char[9];
        private char currentPlayer = 'X';
        private bool isGameOver = false;
        private int playerScore = 0;
        private int computerScore = 0;
        private Random random = new Random();

        public TicTacToeWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
            ClearBoard();
        }

        private void ClearBoard()
        {
            for (int i = 0; i < 9; i++)
            {
                board[i] = ' ';
            }
            currentPlayer = 'X';
            isGameOver = false;
            txtStatus.Text = "你的回合 (X)";
            txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));

            foreach (var child in boardGrid.Children)
            {
                if (child is Button btn)
                {
                    btn.Content = "";
                    btn.IsEnabled = true;
                }
            }
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (isGameOver) return;

            var btn = sender as Button;
            if (btn == null || btn.Tag == null) return;
            int index = int.Parse(btn.Tag.ToString()!);

            if (board[index] != ' ') return;

            MakeMove(index, 'X');

            if (!isGameOver)
            {
                ComputerMove();
            }
        }

        private void MakeMove(int index, char player)
        {
            board[index] = player;
            var btn = boardGrid.Children[index * 2] as Button;
            if (btn == null) return;
            btn.Content = player.ToString();
            btn.FontSize = 48;
            btn.FontWeight = FontWeights.Bold;
            btn.Foreground = player == 'X'
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            btn.IsEnabled = false;

            var result = CheckWinner();
            if (result != ' ')
            {
                GameOver(result);
            }
            else if (IsBoardFull())
            {
                isGameOver = true;
                txtStatus.Text = "平局!";
                txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            }
            else
            {
                currentPlayer = currentPlayer == 'X' ? 'O' : 'X';
                if (currentPlayer == 'X')
                {
                    txtStatus.Text = "你的回合 (X)";
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                }
                else
                {
                    txtStatus.Text = "电脑思考中...";
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                }
            }
        }

        private void ComputerMove()
        {
            isGameOver = true; // 暂时阻止玩家点击

            var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            delay.Tick += (s, e) =>
            {
                delay.Stop();

                int move = GetBestMove();
                MakeMove(move, 'O');

                if (!isGameOver)
                {
                    isGameOver = false; // 恢复玩家回合
                }
            };
            delay.Start();
        }

        private int GetBestMove()
        {
            // 简单AI: 优先获胜，其次防守，随机选择
            // 检查能否获胜
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    board[i] = 'O';
                    if (CheckWinner() == 'O')
                    {
                        board[i] = ' ';
                        return i;
                    }
                    board[i] = ' ';
                }
            }

            // 检查需要防守
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    board[i] = 'X';
                    if (CheckWinner() == 'X')
                    {
                        board[i] = ' ';
                        return i;
                    }
                    board[i] = ' ';
                }
            }

            // 占据中心
            if (board[4] == ' ') return 4;

            // 占据角落
            int[] corners = { 0, 2, 6, 8 };
            foreach (int i in corners)
            {
                if (board[i] == ' ') return i;
            }

            // 随机选择
            int[] available = new int[9];
            int count = 0;
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ')
                {
                    available[count++] = i;
                }
            }
            return available[random.Next(0, count)];
        }

        private char CheckWinner()
        {
            int[,] lines = {
                {0, 1, 2}, {3, 4, 5}, {6, 7, 8}, // 行
                {0, 3, 6}, {1, 4, 7}, {2, 5, 8}, // 列
                {0, 4, 8}, {2, 4, 6}              // 对角线
            };

            for (int i = 0; i < 8; i++)
            {
                int a = lines[i, 0], b = lines[i, 1], c = lines[i, 2];
                if (board[a] != ' ' && board[a] == board[b] && board[b] == board[c])
                {
                    return board[a];
                }
            }
            return ' ';
        }

        private bool IsBoardFull()
        {
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == ' ') return false;
            }
            return true;
        }

        private void GameOver(char winner)
        {
            isGameOver = true;

            if (winner == 'X')
            {
                playerScore++;
                txtPlayerScore.Text = playerScore.ToString();
                txtStatus.Text = "你赢了!";
                txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }
            else
            {
                computerScore++;
                txtComputerScore.Text = computerScore.ToString();
                txtStatus.Text = "电脑赢了!";
                txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            }
        }

        private void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            ClearBoard();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
