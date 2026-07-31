using OVCMOVE.Application.Features.Teams.Query.ScoreHistory;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IScoringLogRepository
{
    Task<(
        IReadOnlyCollection<ScoreHistoryItemResultModel> Items,
        int TotalItems)> GetPageAsync(
            Guid raceId,
            Guid teamId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

    Task<CompletedBoothStats> GetCompletedBoothStatsAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default);
}

public sealed record CompletedBoothStats(
    int CompletedRegularBooths,
    int CompletedHiddenBooths);
