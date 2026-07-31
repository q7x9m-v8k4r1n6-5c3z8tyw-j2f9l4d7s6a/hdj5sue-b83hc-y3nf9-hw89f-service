using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Teams.Query.ScoreHistory;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public sealed class ScoringLogRepository(IDbExecutor db)
    : IScoringLogRepository
{
    public async Task<(
        IReadOnlyCollection<ScoreHistoryItemResultModel> Items,
        int TotalItems)> GetPageAsync(
            Guid raceId,
            Guid teamId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new
        {
            RaceId = raceId,
            TeamId = teamId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };
        var items = await db.QueryAsync<ScoreHistoryItemResultModel>(
            ScoringLogQueries.GetPageQuery(),
            parameters,
            cancellationToken);
        var totalItems = await db.QueryFirstOrDefaultAsync<int>(
            ScoringLogQueries.CountQuery(),
            new { RaceId = raceId, TeamId = teamId },
            cancellationToken);

        return (items.ToArray(), totalItems);
    }

    public async Task<CompletedBoothStats> GetCompletedBoothStatsAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await db.QueryFirstOrDefaultAsync<CompletedBoothStats>(
            ScoringLogQueries.GetCompletedBoothStatsQuery(),
            new { RaceId = raceId, TeamId = teamId },
            cancellationToken)
            ?? new CompletedBoothStats(0, 0);
    }
}
