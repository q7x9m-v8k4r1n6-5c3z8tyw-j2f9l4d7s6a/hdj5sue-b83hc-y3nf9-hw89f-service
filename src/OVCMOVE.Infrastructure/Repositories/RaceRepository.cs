using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class RaceRepository : BaseRepository<RaceRepository>, IRaceRepository
{
    public RaceRepository(ILogger<RaceRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<Guid?> CreateAsync(Race race, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RaceQueries.CreateRaceQuery(), race);
        return affectedRows >= 1 ? race.Id : null;
    }

    public async Task<IReadOnlyCollection<RaceListItemResultModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var races = await _dapperHelper.QueryAsync<RaceListItemResultModel>(RaceQueries.GetAllRacesQuery());
        return races.ToArray();
    }

    public async Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var race = await _dapperHelper.QueryFirstOrDefaultAsync<RaceDetailResultModel>(
            RaceQueries.GetRaceDetailQuery(),
            new { RaceId = raceId });

        if (race is null) return null;

        var booths = await _dapperHelper.QueryAsync<RaceDto.BoothInput>(
            RaceQueries.GetRaceBoothsQuery(),
            new { RaceId = raceId });

        var teams = await _dapperHelper.QueryAsync<RaceDto.RaceTeamInputDto>(
            RaceQueries.GetRaceTeamsQuery(),
            new { RaceId = raceId });

        var organizers = await _dapperHelper.QueryAsync<Guid>(
            RaceQueries.GetRaceOrganizersQuery(),
            new { RaceId = raceId });

        return new RaceDetailResultModel
        {
            Id = race.Id,
            Name = race.Name,
            RaceName = race.RaceName,
            TimeStart = race.TimeStart,
            TimeEnd = race.TimeEnd,
            Place = race.Place,
            Status = race.Status,
            CoverUrl = race.CoverUrl,
            IsToggledLeaderboard = race.IsToggledLeaderboard,
            IsHiddenPoint = race.IsHiddenPoint,
            Booth = booths.ToArray(),
            RaceTeam = teams.ToArray(),
            OrganizerId = organizers.ToArray()
        };
    }

    public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.QueryFirstOrDefaultAsync<Race>(
            RaceQueries.GetRaceByIdQuery(),
            new { RaceId = raceId });
    }

    public async Task<bool> UpdateAsync(Race race, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RaceQueries.UpdateRaceQuery(), race);
        return affectedRows >= 1;
    }
}
