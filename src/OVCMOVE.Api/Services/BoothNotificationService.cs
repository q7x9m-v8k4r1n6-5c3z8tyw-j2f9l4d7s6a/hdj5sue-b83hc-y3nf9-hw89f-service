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
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group(raceId.ToString())
            .ReceiveBoothStatusChanged(boothId, status, teamId);
    }
}