using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Infrastructure.Repositories;

public class RaceOrganizerRepository : BaseRepository<RaceOrganizerRepository>, IRaceOrganizerRepository
{
    public RaceOrganizerRepository(ILogger<RaceOrganizerRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<Guid?> CreateAsync(RaceOrganizer raceOrganizer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RaceQueries.CreateRaceOrganizerQuery(), raceOrganizer);
        return affectedRows >= 1 ? raceOrganizer.Id : null;
    }

    public async Task<IReadOnlyCollection<Guid>> GetOrganizerIdsByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var organizerIds = await _dapperHelper.QueryAsync<Guid>(RaceQueries.GetRaceOrganizersQuery(), new { RaceId = raceId });
        return organizerIds.ToArray();
    }

    public Task DeleteAsync(Guid raceId, Guid organizerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.ExecuteAsync(RaceQueries.DeleteRaceOrganizerQuery(), new { RaceId = raceId, OrganizerId = organizerId });
    }

    public Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.ExecuteAsync(RaceQueries.DeleteRaceOrganizersByRaceIdQuery(), new { RaceId = raceId });
    }
}
