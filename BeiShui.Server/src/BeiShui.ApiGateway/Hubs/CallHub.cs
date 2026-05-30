using Microsoft.AspNetCore.SignalR;

namespace BeiShui.ApiGateway.Hubs;

/// <summary>
/// 语音/屏幕分享信令 Hub（供 ScreenShareWindow / VoiceCallWindow 使用）
/// </summary>
public class CallHub : Hub
{
    public async Task JoinChannel(string channelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        await Clients.OthersInGroup(channelId).SendAsync("UserJoined", Context.UserIdentifier ?? "anonymous");
    }

    public async Task LeaveChannel(string channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        await Clients.OthersInGroup(channelId).SendAsync("UserLeft", Context.UserIdentifier ?? "anonymous");
    }

    public async Task SendSignal(string channelId, string signalType, string data, string targetId)
    {
        if (!string.IsNullOrEmpty(targetId))
        {
            // 发给指定用户
            await Clients.Client(targetId).SendAsync("ReceiveSignal", Context.UserIdentifier, signalType, data);
        }
        else
        {
            // 广播给频道内其他人
            await Clients.OthersInGroup(channelId).SendAsync("ReceiveSignal", Context.UserIdentifier, signalType, data);
        }
    }

    public async Task StartScreenShare(string channelId)
    {
        await Clients.OthersInGroup(channelId).SendAsync("ScreenShareStarted", Context.UserIdentifier);
    }

    public async Task StopScreenShare(string channelId)
    {
        await Clients.OthersInGroup(channelId).SendAsync("ScreenShareStopped", Context.UserIdentifier);
    }
}
