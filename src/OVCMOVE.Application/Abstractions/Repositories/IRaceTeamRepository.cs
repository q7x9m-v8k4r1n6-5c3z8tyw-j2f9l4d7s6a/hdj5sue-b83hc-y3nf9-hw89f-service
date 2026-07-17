using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

/// <summary>
/// Interface cho khối điều khiển lưu trữ Đội đua trong Giải đấu.
/// </summary>
public interface IRaceTeamRepository
{
    Task<Guid?> CreateAsync(RaceTeam raceTeam, CancellationToken cancellationToken = default);
    Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
}
