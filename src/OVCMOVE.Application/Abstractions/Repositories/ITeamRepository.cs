using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface ITeamRepository
{
    Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IEnumerable<Guid> teamIds,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<User> Items, int TotalItems)> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<User>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(User team, CancellationToken cancellationToken = default);
    
    Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(
        Guid? raceId, 
        CancellationToken cancellationToken = default);
}
