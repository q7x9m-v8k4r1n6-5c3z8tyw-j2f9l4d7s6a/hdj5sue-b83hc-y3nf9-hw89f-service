using OVCMOVE.Application.Features.Races.Common;

namespace OVCMOVE.Application.Abstractions.Hubs;

public interface IBoothHubClient
{
    /// <summary>
    /// Phat su kien doi trang thai tram ve cho tat ca App/Web dang xem ban do.
    /// </summary>
    Task ReceiveBoothStatusChanged(Guid boothId, string status, Guid? teamId, string? teamName);

    /// <summary>
    /// Phat su kien diem cua mot doi trong tran dau vua thay doi.
    /// </summary>
    Task ReceiveRaceScoreChanged(Guid raceId, Guid teamId, int delta);

    Task ReceiveBoothEntryCancelled(Guid boothId, Guid teamId);

    Task ReceiveBoothEntryRejected(Guid boothId, Guid teamId);

    Task ReceiveRaceMessage(RaceMessageResultModel message);
}
