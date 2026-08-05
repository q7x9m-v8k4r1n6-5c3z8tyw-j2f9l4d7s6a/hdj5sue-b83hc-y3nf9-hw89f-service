using Microsoft.AspNetCore.SignalR;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Abstractions.Hubs;

namespace OVCMOVE.API.Hubs;

[RequirePermission(PermissionCodes.RaceRead)]
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

    public async Task JoinRaceGroup(string raceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Race_{raceId}");
    }

    public async Task LeaveRaceGroup(string raceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Race_{raceId}");
    }
}
