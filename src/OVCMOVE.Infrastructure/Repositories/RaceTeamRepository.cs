using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Infrastructure.Repositories;

public class RaceTeamRepository : BaseRepository<RaceTeamRepository>, IRaceTeamRepository
{
    public RaceTeamRepository(ILogger<RaceTeamRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<Guid?> CreateAsync(RaceTeam raceTeam, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RaceQueries.CreateRaceTeamQuery(), raceTeam);
        return affectedRows >= 1 ? raceTeam.Id : null;
    }

    public async Task<IReadOnlyCollection<Guid>> GetTeamIdsByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var teamIds = await _dapperHelper.QueryAsync<Guid>(RaceQueries.GetRaceTeamsQuery(), new { RaceId = raceId });
        return teamIds.ToArray();
    }

    public Task DeleteAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.ExecuteAsync(RaceQueries.DeleteRaceTeamQuery(), new { RaceId = raceId, TeamId = teamId });
    }

    public Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.ExecuteAsync(RaceQueries.DeleteRaceTeamsByRaceIdQuery(), new { RaceId = raceId });
    }
}
