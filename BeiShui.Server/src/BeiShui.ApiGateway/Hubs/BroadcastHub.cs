using Microsoft.AspNetCore.SignalR;

namespace BeiShui.ApiGateway.Hubs;

public class BroadcastHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[BroadcastHub] 客户端已连接: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[BroadcastHub] 客户端已断开: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
