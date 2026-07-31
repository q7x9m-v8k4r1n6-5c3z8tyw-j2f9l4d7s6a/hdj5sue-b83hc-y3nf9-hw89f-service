using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public sealed class BoothOrganizerRepository : IBoothOrganizerRepository
{
    private readonly IDbExecutor _db;

    public BoothOrganizerRepository(IDbExecutor db)
    {
        _db = db;
    }

    /// <summary>Creates one validated booth-organizer relationship.</summary>
    public async Task CreateAsync(
        BoothOrganizer boothOrganizer,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.CreateBoothOrganizerQuery(),
            boothOrganizer,
            cancellationToken);
        PersistenceWriteGuard.EnsureInserted(
            affectedRows,
            nameof(BoothOrganizer));
    }

    /// <summary>Removes every organizer relationship owned by one booth.</summary>
    public async Task DeleteByBoothIdAsync(
        Guid boothId,
        CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync(
            RaceQueries.DeleteBoothOrganizersByBoothIdQuery(),
            new { BoothId = boothId },
            cancellationToken);
    }

    public async Task<BoothOrganizer?> GetByOrganizerIdAsync(
    Guid organizerId,
    CancellationToken cancellationToken = default)
    {
        return await _db.QueryFirstOrDefaultAsync<BoothOrganizer>(
            RaceQueries.GetBoothOrganizerByOrganizerIdQuery(),
            new { OrganizerId = organizerId },
            cancellationToken);
    }
}
