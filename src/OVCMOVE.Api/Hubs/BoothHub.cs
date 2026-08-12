using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Hubs;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.API.Hubs;

[RequirePermission(PermissionCodes.RaceRead)]
public class BoothHub(
    IRaceRepository raceRepository,
    IBoothOrganizerRepository boothOrganizerRepository)
    : Hub<IBoothHubClient>
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

    public async Task JoinRaceMessageGroups(string raceId)
    {
        if (!Guid.TryParse(raceId, out var raceGuid))
        {
            throw new HubException("RaceId không hợp lệ.");
        }

        var userId = GetRequiredUserId();
        var userType = GetUserType();

        if (string.Equals(userType, UserConstants.UserType.Team, StringComparison.OrdinalIgnoreCase))
        {
            if (!await raceRepository.IsTeamInRaceAsync(raceGuid, userId, Context.ConnectionAborted))
            {
                throw new HubException("Bạn chưa được gán vào trận đấu này.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, RaceMessageHubGroups.All(raceGuid));
            await Groups.AddToGroupAsync(Context.ConnectionId, RaceMessageHubGroups.AllTeams(raceGuid));
            await Groups.AddToGroupAsync(Context.ConnectionId, RaceMessageHubGroups.Team(raceGuid, userId));
            return;
        }

        if (string.Equals(userType, UserConstants.UserType.Organizer, StringComparison.OrdinalIgnoreCase))
        {
            var boothOrganizer = await boothOrganizerRepository.GetByOrganizerAndRaceAsync(
                userId,
                raceGuid,
                Context.ConnectionAborted);
            if (boothOrganizer is null)
            {
                throw new HubException("Bạn chưa được gán vào trận đấu này.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, RaceMessageHubGroups.All(raceGuid));
            await Groups.AddToGroupAsync(Context.ConnectionId, RaceMessageHubGroups.AllOrganizers(raceGuid));
            await Groups.AddToGroupAsync(Context.ConnectionId, RaceMessageHubGroups.Organizer(raceGuid, userId));
            return;
        }

        throw new HubException("Tài khoản không thể nhận tin nhắn trận đấu.");
    }

    public async Task LeaveRaceMessageGroups(string raceId)
    {
        if (!Guid.TryParse(raceId, out var raceGuid))
        {
            return;
        }

        var userId = GetCurrentUserId();
        var userType = GetUserType();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RaceMessageHubGroups.All(raceGuid));
        if (userId is null) return;

        if (string.Equals(userType, UserConstants.UserType.Team, StringComparison.OrdinalIgnoreCase))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RaceMessageHubGroups.AllTeams(raceGuid));
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RaceMessageHubGroups.Team(raceGuid, userId.Value));
            return;
        }

        if (string.Equals(userType, UserConstants.UserType.Organizer, StringComparison.OrdinalIgnoreCase))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RaceMessageHubGroups.AllOrganizers(raceGuid));
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RaceMessageHubGroups.Organizer(raceGuid, userId.Value));
        }
    }

    private Guid GetRequiredUserId() =>
        GetCurrentUserId() ?? throw new HubException("Không xác định được người dùng.");

    private Guid? GetCurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private string GetUserType() =>
        Context.User?.FindFirst("user_type")?.Value ?? string.Empty;
}
