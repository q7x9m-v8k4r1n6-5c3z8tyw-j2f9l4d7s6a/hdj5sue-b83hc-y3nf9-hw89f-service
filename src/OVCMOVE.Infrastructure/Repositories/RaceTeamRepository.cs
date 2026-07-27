using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class RaceTeamRepository : IRaceTeamRepository
{
    private readonly IDbExecutor _db;

    public RaceTeamRepository(IDbExecutor db) =>
        _db = db;

    public async Task CreateAsync(RaceTeam raceTeam, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.CreateRaceTeamQuery(),
            raceTeam,
            cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(
            affectedRows,
            nameof(RaceTeam));
    }

    public async Task<IReadOnlyCollection<Guid>> GetTeamIdsByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var teamIds = await _db.QueryAsync<Guid>(
            RaceQueries.GetRaceTeamIdsQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);
        return teamIds.ToArray();
    }

    public Task DeleteAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.ExecuteAsync(
            RaceQueries.DeleteRaceTeamQuery(),
            new { RaceId = raceId, TeamId = teamId },
            cancellationToken: cancellationToken);
    }

}
