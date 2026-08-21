using OVCMOVE.Application.Features.Races.Common;

namespace OVCMOVE.Application.Abstractions.Hubs;

public interface IBoothHubClient
{
    /// <summary>
    /// Phát sự kiện đổi trạng thái trạm về cho tất cả App/Web đang xem bản đồ
    /// </summary>
    Task ReceiveBoothStatusChanged(Guid boothId, string status, Guid? teamId, string? teamName);

    /// <summary>
    /// Phát sự kiện điểm của một đội trong trận đấu vừa thay đổi.
    /// </summary>
    Task ReceiveRaceScoreChanged(Guid raceId, Guid teamId, int delta);

    Task ReceiveBoothEntryCancelled(Guid boothId, Guid teamId);

    Task ReceiveBoothEntryRejected(Guid boothId, Guid teamId);

    Task ReceiveRaceMessage(RaceMessageResultModel message);

    Task ReceiveBoothCompleted(
        Guid boothId,
        Guid teamId,
        string boothName,
        int score);
}
