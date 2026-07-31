using Microsoft.AspNetCore.SignalR;
using OVCMOVE.Application.Abstractions.Hubs;

namespace OVCMOVE.API.Hubs;

public class BoothHub : Hub<IBoothHubClient>
{
    public async Task JoinBoothGroup(string boothId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Booth_{boothId}");
    }

    public async Task LeaveBoothGroup(string boothId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Booth_{boothId}");
    }
}