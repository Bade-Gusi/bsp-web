using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using BeiShui.ApiGateway.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Hubs;

public class ChatHub : Hub
{
    private readonly UserConnectionManager _connectionManager;
    private readonly AppDbContext _db;

    public ChatHub(UserConnectionManager connectionManager, AppDbContext db)
    {
        _connectionManager = connectionManager;
        _db = db;
    }

    public async Task SendPrivateMessage(long toUserId, string content)
    {
        var fromUserId = Context.UserIdentifier;
        var fromUser = await _db.Users.FindAsync(long.Parse(fromUserId ?? "0"));

        var connectionId = await _connectionManager.GetConnectionId(toUserId);
        if (connectionId != null)
        {
            await Clients.Client(connectionId).SendAsync("OnPrivateMessage", new
            {
                FromUserId = fromUserId,
                FromName = fromUser?.Nickname ?? fromUser?.Username ?? "unknown",
                Content = content,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task SendRoomMessage(string roomCode, string content)
    {
        var fromUserId = Context.UserIdentifier;
        var fromUser = await _db.Users.FindAsync(long.Parse(fromUserId ?? "0"));

        await Clients.Group($"room:{roomCode}").SendAsync("OnRoomMessage", new
        {
            FromUserId = fromUserId,
            FromName = fromUser?.Nickname ?? fromUser?.Username ?? "unknown",
            Content = content,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task JoinRoom(string roomCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomCode}");
    }

    public async Task LeaveRoom(string roomCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomCode}");
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.UserIdentifier;
        if (long.TryParse(userIdStr, out var userId))
        {
            await _connectionManager.SetConnectionId(userId, Context.ConnectionId);
            await NotifyFriendsStatus(userId, true);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = Context.UserIdentifier;
        if (long.TryParse(userIdStr, out var userId))
        {
            await _connectionManager.RemoveConnection(userId);
            await NotifyFriendsStatus(userId, false);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 通知好友上下线
    /// </summary>
    private async Task NotifyFriendsStatus(long userId, bool isOnline)
    {
        // 查找所有将 userId 加为好友的用户
        var friendRelations = await _db.Friends
            .Where(f => f.FriendId == userId && f.Status == 1)
            .Select(f => f.UserId)
            .ToListAsync();

        foreach (var friendId in friendRelations)
        {
            var connId = await _connectionManager.GetConnectionId(friendId);
            if (connId != null)
            {
                await Clients.Client(connId).SendAsync("OnFriendStatusChanged", new
                {
                    UserId = userId.ToString(),
                    IsOnline = isOnline
                });
            }
        }
    }
}
