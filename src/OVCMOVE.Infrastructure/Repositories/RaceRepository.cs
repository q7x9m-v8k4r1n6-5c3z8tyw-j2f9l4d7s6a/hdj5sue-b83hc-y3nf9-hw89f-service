using System.Data;
using Dapper;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class RaceRepository : IRaceRepository
{
    private readonly IDbExecutor _db;

    public RaceRepository(IDbExecutor db) =>
        _db = db;

    public async Task CreateAsync(Race race, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.CreateRaceQuery(),
            race,
            cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(Race));
    }

    public async Task<(
        IReadOnlyCollection<RaceItemResultModel> Items,
        int TotalItems)> GetPageAsync(
        int page,
        int pageSize,
        Guid? teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var races = await _db.QueryAsync<RaceItemResultModel>(
            RaceQueries.GetAllRacesQuery(),
            new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize,
                TeamId = teamId
            },
            cancellationToken: cancellationToken);
        var totalItems = await _db.QueryFirstOrDefaultAsync<int>(
            RaceQueries.CountRacesQuery(),
            new { TeamId = teamId },
            cancellationToken: cancellationToken);

        return (races.ToArray(), totalItems);
    }

    public async Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var race = await _db.QueryFirstOrDefaultAsync<RaceDetailResultModel>(
            RaceQueries.GetRaceDetailQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);

        if (race is null) return null;

        var boothRows = await _db.QueryAsync<RaceBoothModel>(
            RaceQueries.GetRaceBoothsQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);
        var boothOrganizers = await _db.QueryAsync<BoothOrganizerRow>(
            RaceQueries.GetRaceBoothOrganizersQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);
        var organizerIdsByBooth = boothOrganizers
            .GroupBy(item => item.BoothId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<Guid>)group
                    .Select(item => item.OrganizerId)
                    .ToArray());
        var booths = boothRows.Select(booth => new RaceBoothModel
        {
            Id = booth.Id,
            Name = booth.Name,
            Place = booth.Place,
            Description = booth.Description,
            OrganizerIds = organizerIdsByBooth.GetValueOrDefault(
                booth.Id,
                Array.Empty<Guid>())
        }).ToArray();

        var teams = await _db.QueryAsync<RaceTeamModel>(
            RaceQueries.GetRaceTeamsQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);

        var organizerIds = await _db.QueryAsync<Guid>(
            RaceQueries.GetRaceOrganizersQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);

        var organizers = await _db.QueryAsync<RaceOrganizerModel>(
            RaceQueries.GetRaceOrganizerDetailsQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);

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
            ModifiedAt = race.ModifiedAt,
            IsToggledLeaderboard = race.IsToggledLeaderboard,
            IsHiddenPoint = race.IsHiddenPoint,
            Booth = booths,
            RaceTeam = teams.ToArray(),
            OrganizerId = organizerIds.ToArray(),
            Organizers = organizers.ToArray()
        };
    }

    public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.QueryFirstOrDefaultAsync<Race>(
            RaceQueries.GetRaceByIdQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Race race,
        DateTime expectedModifiedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new DynamicParameters(new
        {
            race.Id,
            race.RaceName,
            race.TimeStart,
            race.TimeEnd,
            race.Place,
            race.Status,
            race.Rules,
            race.IsToggledLeaderboard,
            race.IsHiddenPoint,
            race.CoverUrl,
            race.ModifiedBy,
            race.ModifiedAt
        });
        parameters.Add(
            "ExpectedModifiedAt",
            expectedModifiedAt,
            DbType.DateTime2);

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.UpdateRaceQuery(),
            parameters,
            cancellationToken: cancellationToken);
        return affectedRows >= 1;
    }

    public async Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(
        Guid? raceId, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string sqlQuery = RaceQueries.GetTeamLeaderboardQuery();
        var parameters = new { RaceId = raceId };
        var result = await _db.QueryAsync<TeamLeaderboardResultModel>(
            sqlQuery,
            parameters,
            cancellationToken);
        return result.ToList();
    }

    public async Task<List<BoothListResultModel>> GetBoothListAsync(
        Guid? raceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string sqlQuery = RaceQueries.GetBoothListQuery();
        var parameters = new { RaceId = raceId };
        var result = await _db.QueryAsync<BoothListResultModel>(sqlQuery, parameters);
        return result.ToList();
    }

    public async Task<(
        IReadOnlyCollection<ScoringLogResultModel> Items, 
        int TotalItems)> GetScoringLogPageByRaceIdAsync(
            Guid raceId,
            Guid? teamId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var logs = await _db.QueryAsync<ScoringLogResultModel>(
            RaceQueries.GetScoringLogByRaceIdQuery(),
            new
            {
                RaceId = raceId,
                TeamId = teamId,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken);

        var totalItems = await _db.QueryFirstOrDefaultAsync<int>(
            RaceQueries.CountScoringLogByRaceIdQuery(),
            new { RaceId = raceId, TeamId = teamId },
            cancellationToken: cancellationToken);

        return (logs.ToArray(), totalItems);
    }

    public async Task<(
        int CompletedRegularBooths,
        int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(
            Guid raceId,
            Guid teamId,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stats = await _db.QueryFirstOrDefaultAsync<CompletedBoothStatsRow>(
            RaceQueries.GetCompletedBoothStatsQuery(),
            new { RaceId = raceId, TeamId = teamId },
            cancellationToken: cancellationToken);

        return stats is null
            ? (0, 0)
            : (stats.CompletedRegularBooths, stats.CompletedHiddenBooths);
    }

    public Task<int?> GetRaceTeamScoreAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.QueryFirstOrDefaultAsync<int?>(
            RaceQueries.GetRaceTeamScoreQuery(),
            new { RaceId = raceId, TeamId = teamId },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateRaceTeamScoreAsync(
        Guid raceId,
        Guid teamId,
        int totalScore,
        string modifiedBy,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.UpdateRaceTeamScoreQuery(),
            new
            {
                RaceId = raceId,
                TeamId = teamId,
                TotalScore = totalScore,
                ModifiedBy = modifiedBy,
                ModifiedAt = modifiedAt
            },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }

    public async Task CreateScoringLogAsync(
        ScoringLog log,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.CreateScoringLogQuery(),
            log,
            cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(ScoringLog));
    }

    public async Task<bool> IsTeamInRaceAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _db.QueryFirstOrDefaultAsync<int>(
            RaceQueries.CheckTeamInRaceQuery(),
            new { RaceId = raceId, TeamId = teamId },
            cancellationToken: cancellationToken);

        return result == 1;
    }

    public Task<string?> GetRulesAsync(
        Guid raceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.QueryFirstOrDefaultAsync<string?>(
            RaceQueries.GetRaceRulesQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);
    }
}

internal sealed class BoothOrganizerRow
{
    public Guid BoothId { get; init; }
    public Guid OrganizerId { get; init; }
}

internal sealed class CompletedBoothStatsRow
{
    public int CompletedRegularBooths { get; init; }
    public int CompletedHiddenBooths { get; init; }
}
