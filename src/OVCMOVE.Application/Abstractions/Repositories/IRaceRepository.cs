using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRaceRepository
{
    Task CreateAsync(Race race, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)>
        GetPageAsync(
            int page,
            int pageSize,
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
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
