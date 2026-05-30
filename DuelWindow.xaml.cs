using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BeiShuiCS2.Services;

namespace BeiShuiCS2;

public partial class DuelWindow : Window
{
    private DispatcherTimer? _matchTimer;
    private int _elapsedSeconds;
    private bool _isSearching;
    private string? _currentMatchId;
    private string? _connectAddress;

    public ObservableCollection<DuelFriend> Friends { get; set; } = new();

    public DuelWindow()
    {
        InitializeComponent();
        DataContext = this;

        Loaded += async (s, e) =>
        {
            AnimationHelper.CreateFloatingParticles(particleCanvas, 8);
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));

            // 加载好友列表
            await LoadFriends();

            // 注册 SignalR 事件
            RegisterSignalRHandlers();
        };

        MouseLeftButtonDown += (s, e) =>
        {
            if (e.GetPosition(this).Y < 56) DragMove();
        };
    }

    private void RegisterSignalRHandlers()
    {
        // 注意: 这里通过 App.SignalR 注册，实际 SignalR 连接在登录时建立
        // 匹配成功回调
        // App.SignalR.OnDuelMatchFound(OnMatchFound);

        // 由于 SignalRService 的 On 注册方式，需要在连接前注册
        // 当前简化处理：匹配结果通过 API 轮询或 mock
    }

    private async System.Threading.Tasks.Task LoadFriends()
    {
        try
        {
            var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/friends");
            if (result.Success && result.Data != null)
            {
                Friends.Clear();
                foreach (var f in result.Data)
                {
                    Friends.Add(new DuelFriend
                    {
                        Name = f.GetProperty("nickname").GetString() ?? f.GetProperty("username").GetString() ?? "",
                        Id = f.GetProperty("id").GetInt32()
                    });
                }
                friendList.ItemsSource = Friends;
            }
        }
        catch { }
    }

    // ===== Tab 切换 =====
    private void TabMatch_Click(object sender, RoutedEventArgs e)
    {
        panelMatch.Visibility = Visibility.Visible;
        panelFriend.Visibility = Visibility.Collapsed;
    }

    private void TabFriend_Click(object sender, RoutedEventArgs e)
    {
        panelMatch.Visibility = Visibility.Collapsed;
        panelFriend.Visibility = Visibility.Visible;
    }

    // ===== 随机匹配 =====
    private async void StartMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_isSearching) return;
        _isSearching = true;
        _elapsedSeconds = 0;

        btnStartMatch.Visibility = Visibility.Collapsed;
        matchStatusPanel.Visibility = Visibility.Visible;
        opponentPanel.Visibility = Visibility.Collapsed;
        serverStartingPanel.Visibility = Visibility.Collapsed;

        txtMatchStatus.Text = "正在加入匹配队列...";

        // 加入匹配队列
        try
        {
            var result = await ApiClient.PostAsync<System.Text.Json.JsonElement>("/api/duel/queue/join", new { });
            if (!result.Success)
            {
                ShowError("加入匹配失败");
                return;
            }
        }
        catch
        {
            // 如果后端不支持，使用模拟匹配
        }

        txtMatchStatus.Text = "搜索中...";

        // 启动计时器
        _matchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _matchTimer.Tick += (_, _) =>
        {
            _elapsedSeconds++;
            txtMatchTimer.Text = $"{_elapsedSeconds / 60:D2}:{_elapsedSeconds % 60:D2}";

            // 模拟匹配（实际应通过 SignalR 回调）
            if (_elapsedSeconds == 5)
            {
                _matchTimer?.Stop();
                OnMatchFound("神秘对手", 1200);
            }
        };
        _matchTimer.Start();
    }

    private void OnMatchFound(string opponentName, int opponentMMR)
    {
        Dispatcher.Invoke(() =>
        {
            matchStatusPanel.Visibility = Visibility.Collapsed;
            opponentPanel.Visibility = Visibility.Visible;
            txtOpponentName.Text = opponentName;
            txtOpponentMMR.Text = $"MMR: {opponentMMR}";
            _currentMatchId = $"duel_{DateTime.UtcNow.Ticks}";
        });
    }

    private async void AcceptDuel_Click(object sender, RoutedEventArgs e)
    {
        btnAcceptDuel.IsEnabled = false;
        opponentPanel.Visibility = Visibility.Collapsed;
        serverStartingPanel.Visibility = Visibility.Visible;

        var result = await ApiClient.PostAsync<System.Text.Json.JsonElement>("/api/duel/accept/0", new { });

        if (result.Success && result.Data.ValueKind != System.Text.Json.JsonValueKind.Undefined)
        {
            try { _connectAddress = result.Data.GetProperty("connectAddress").GetString(); } catch { }
        }

        if (!string.IsNullOrEmpty(_connectAddress))
            LaunchGame();
    }

    private void RejectDuel_Click(object sender, RoutedEventArgs e)
    {
        opponentPanel.Visibility = Visibility.Collapsed;
        matchStatusPanel.Visibility = Visibility.Collapsed;
        _isSearching = false;
        btnStartMatch.Visibility = Visibility.Visible;
    }

    // ===== 好友邀约 =====
    private async void InviteDuel_Click(object sender, RoutedEventArgs e)
    {
        var selected = friendList.SelectedItem as DuelFriend;
        if (selected == null)
        {
            txtInviteStatus.Text = "请先选择一个好友";
            return;
        }

        btnInvite.IsEnabled = false;
        txtInviteStatus.Text = $"正在向 {selected.Name} 发起邀约...";

        try
        {
            var result = await ApiClient.PostAsync<System.Text.Json.JsonElement>("/api/duel/invite", new
            {
                toUserId = selected.Id,
                mapName = "de_dust2"
            });

            if (result.Success)
            {
                txtInviteStatus.Text = $"✅ 已向 {selected.Name} 发送邀约，等待对方接受...";
            }
            else
            {
                txtInviteStatus.Text = $"❌ {result.Error ?? "邀约失败"}";
            }
        }
        catch (Exception ex)
        {
            txtInviteStatus.Text = $"❌ 邀约失败: {ex.Message}";
        }
        finally
        {
            btnInvite.IsEnabled = true;
        }
    }

    // ===== 启动游戏 =====
    private void LaunchGame()
    {
        if (string.IsNullOrEmpty(_connectAddress)) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin?.ShowToast("🎮 对战开始！正在连接服务器...");

            GameLauncher.ConnectAndHeartbeat(_connectAddress);
            AnimationHelper.WindowCloseWithScale(this, () => Close());
        });
    }

    private void ShowError(string msg)
    {
        txtMatchStatus.Text = $"❌ {msg}";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        // 离开匹配队列
        if (_isSearching)
        {
            _ = ApiClient.PostAsync<System.Text.Json.JsonElement>("/api/duel/queue/leave", new { });
        }
        AnimationHelper.WindowCloseWithScale(this, () => Close());
    }
}

public class DuelFriend
{
    public string Name { get; set; } = "";
    public int Id { get; set; }
}
