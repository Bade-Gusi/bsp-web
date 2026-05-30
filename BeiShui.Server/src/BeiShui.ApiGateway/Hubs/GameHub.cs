using Microsoft.AspNetCore.SignalR;

namespace BeiShui.ApiGateway.Hubs;

public class GameHub : Hub
{
    public async Task JoinRoom(string roomCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomCode}");
    }

    public async Task LeaveRoom(string roomCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomCode}");
    }

    public async Task GameStarted(string roomCode)
    {
        await Clients.Group($"room:{roomCode}").SendAsync("OnGameStarted", new
        {
            RoomCode = roomCode,
            StartedAt = DateTime.UtcNow
        });
    }

    public async Task UpdateScore(string roomCode, int ctScore, int tScore)
    {
        await Clients.Group($"room:{roomCode}").SendAsync("OnScoreUpdate", new
        {
            RoomCode = roomCode,
            CtScore = ctScore,
            TScore = tScore
        });
    }
}
