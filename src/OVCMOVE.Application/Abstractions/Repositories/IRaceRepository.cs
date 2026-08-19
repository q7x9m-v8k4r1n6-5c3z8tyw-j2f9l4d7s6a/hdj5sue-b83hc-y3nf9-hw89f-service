using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Booths.Common;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRaceRepository
{
    Task CreateAsync(Race race, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)>
        GetPageAsync(
            RacePageRequestModel request,
            CancellationToken cancellationToken = default);
    Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(
        Race race,
        DateTime expectedModifiedAt,
        CancellationToken cancellationToken = default);
    Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(
        Guid? raceId, 
        CancellationToken cancellationToken = default);
    Task<List<BoothListResultModel>> GetBoothListAsync(
        Guid? raceId, 
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<ScoringLogResultModel> Items, int TotalItems)> GetScoringLogPageByRaceIdAsync(
        Guid raceId,
        Guid? teamId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(int CompletedRegularBooths, int CompletedHiddenBooths)>
        GetCompletedBoothStatsAsync(
            Guid raceId,
            Guid teamId,
            CancellationToken cancellationToken = default);
    Task<int?> GetRaceTeamScoreAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateRaceTeamScoreAsync(
        Guid raceId,
        Guid teamId,
        int totalScore,
        string modifiedBy,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default);
    Task CreateScoringLogAsync(
        ScoringLog log,
        CancellationToken cancellationToken = default);
    Task CreateRaceMessageAsync(
        RaceMessage message,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(
        Guid raceId,
        int limit,
        CancellationToken cancellationToken = default);
    Task<bool> IsTeamInRaceAsync(
    Guid raceId,
    Guid teamId,
    CancellationToken cancellationToken = default);
    Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<BoothProgressResultModel> GetBoothProgressAsync(
        Guid raceId,
        Guid teamId,
        Guid boothId,
        CancellationToken cancellationToken = default);
}
