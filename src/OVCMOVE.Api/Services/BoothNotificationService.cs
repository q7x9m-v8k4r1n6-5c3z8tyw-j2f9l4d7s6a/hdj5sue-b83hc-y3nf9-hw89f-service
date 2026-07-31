using Microsoft.AspNetCore.SignalR;
using OVCMOVE.API.Hubs;
using OVCMOVE.Application.Abstractions.Hubs;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Api.Services;

public class BoothNotificationService : IBoothNotificationService
{
    private readonly IHubContext<BoothHub, IBoothHubClient> _hubContext;

    public BoothNotificationService(IHubContext<BoothHub, IBoothHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBoothStatusChangedAsync(
        Guid raceId,
        Guid boothId,
        string status,
        Guid? teamId,
        string? teamName,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"Booth_{boothId}")
            .ReceiveBoothStatusChanged(boothId, status, teamId, teamName);
    }

    public async Task NotifyRaceScoreChangedAsync(
        Guid raceId,
        Guid teamId,
        int delta,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"Race_{raceId}")
            .ReceiveRaceScoreChanged(raceId, teamId, delta);
    }
}
