using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

/// <summary>
/// Interface cho khối điều khiển lưu trữ Ban tổ chức thuộc Giải đấu.
/// </summary>
public interface IRaceOrganizerRepository
{
    Task<Guid?> CreateAsync(RaceOrganizer raceOrganizer, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Guid>> GetOrganizerIdsByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid raceId, Guid organizerId, CancellationToken cancellationToken = default);
    Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
}
