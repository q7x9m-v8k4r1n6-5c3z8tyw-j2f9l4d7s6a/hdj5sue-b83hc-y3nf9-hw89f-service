using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class BoothRepository : IBoothRepository
{
    private readonly IDbExecutor _db;

    public BoothRepository(IDbExecutor db) =>
        _db = db;

    public async Task CreateAsync(Booth booth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.CreateBoothQuery(),
            booth,
            cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(Booth));
    }

    public async Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booths = await _db.QueryAsync<Booth>(
            RaceQueries.GetBoothsByRaceIdQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);
        return booths.ToArray();
    }

    public async Task<bool> UpdateAsync(Booth booth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.UpdateBoothQuery(),
            booth,
            cancellationToken: cancellationToken);
        return affectedRows >= 1;
    }

    public Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.ExecuteAsync(
            RaceQueries.DeleteBoothByIdQuery(),
            new { BoothId = boothId },
            cancellationToken: cancellationToken);
    }

}
