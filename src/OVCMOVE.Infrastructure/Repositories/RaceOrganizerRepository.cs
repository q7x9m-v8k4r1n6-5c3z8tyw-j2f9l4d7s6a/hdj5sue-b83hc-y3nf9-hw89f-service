using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class RaceOrganizerRepository : IRaceOrganizerRepository
{
    private readonly IDbExecutor _db;

    public RaceOrganizerRepository(IDbExecutor db) =>
        _db = db;

    public async Task CreateAsync(RaceOrganizer raceOrganizer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.CreateRaceOrganizerQuery(),
            raceOrganizer,
            cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(
            affectedRows,
            nameof(RaceOrganizer));
    }

    public async Task<IReadOnlyCollection<Guid>> GetOrganizerIdsByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var organizerIds = await _db.QueryAsync<Guid>(
            RaceQueries.GetRaceOrganizersQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);
        return organizerIds.ToArray();
    }

    public Task DeleteAsync(Guid raceId, Guid organizerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.ExecuteAsync(
            RaceQueries.DeleteRaceOrganizerQuery(),
            new { RaceId = raceId, OrganizerId = organizerId },
            cancellationToken: cancellationToken);
    }

}
