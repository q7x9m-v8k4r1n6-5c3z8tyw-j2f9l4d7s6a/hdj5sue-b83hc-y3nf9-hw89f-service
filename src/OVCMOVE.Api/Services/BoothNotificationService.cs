using Microsoft.AspNetCore.SignalR;
using OVCMOVE.API.Hubs;
using OVCMOVE.Application.Abstractions.Hubs;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Features.Races.Common;

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

        await _hubContext.Clients
            .Group($"Race_{raceId}")
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

    public async Task NotifyBoothEntryCancelledAsync(
        Guid raceId,
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"Booth_{boothId}")
            .ReceiveBoothEntryCancelled(boothId, teamId);

        await _hubContext.Clients
            .Group($"Race_{raceId}")
            .ReceiveBoothEntryCancelled(boothId, teamId);
    }

    public async Task NotifyBoothEntryRejectedAsync(
        Guid raceId,
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"Booth_{boothId}")
            .ReceiveBoothEntryRejected(boothId, teamId);

        await _hubContext.Clients
            .Group($"Race_{raceId}")
            .ReceiveBoothEntryRejected(boothId, teamId);
    }

    public async Task NotifyRaceMessageAsync(
        Guid raceId,
        RaceMessageResultModel message,
        CancellationToken cancellationToken = default)
    {
        var groups = RaceMessageHubGroups.FromRecipientKeys(
            raceId,
            message.RecipientKeys);

        if (groups.Count > 0)
        {
            await _hubContext.Clients
                .Groups(groups)
                .ReceiveRaceMessage(message);
        }

        await _hubContext.Clients
            .Group(RaceMessageHubGroups.History(raceId))
            .ReceiveRaceMessage(message);
    }

    public Task NotifyBoothCompletedAsync(
        Guid boothId,
        Guid teamId,
        string boothName,
        int score,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .User(teamId.ToString())
            .ReceiveBoothCompleted(boothId, teamId, boothName, score);
}
