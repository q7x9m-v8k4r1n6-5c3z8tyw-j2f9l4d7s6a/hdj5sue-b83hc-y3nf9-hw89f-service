using OVCMOVE.Application.Features.Races.Common;

namespace OVCMOVE.Application.Abstractions.Services;

public interface IBoothNotificationService
{
    /// <summary>
    /// Phát thông báo đổi trạng thái trạm cho các Client thời gian thực
    /// </summary>
    Task NotifyBoothStatusChangedAsync(
        Guid raceId,
        Guid boothId,
        string status,
        Guid? teamId,
        string? teamName,
        CancellationToken cancellationToken = default);

    Task NotifyRaceScoreChangedAsync(
        Guid raceId,
        Guid teamId,
        int delta,
        CancellationToken cancellationToken = default);

    Task NotifyBoothEntryCancelledAsync(
        Guid raceId,
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task NotifyBoothEntryRejectedAsync(
        Guid raceId,
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task NotifyRaceMessageAsync(
        Guid raceId,
        RaceMessageResultModel message,
        CancellationToken cancellationToken = default);
}
