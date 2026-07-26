using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IBoothRepository
{
    Task<Guid?> CreateAsync(Booth booth, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Booth booth, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default);
    Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
}
