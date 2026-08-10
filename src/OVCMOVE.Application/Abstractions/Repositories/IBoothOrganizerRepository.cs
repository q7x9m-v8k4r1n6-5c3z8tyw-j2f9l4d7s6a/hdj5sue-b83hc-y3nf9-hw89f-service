using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IBoothOrganizerRepository
{
    Task CreateAsync(
        BoothOrganizer boothOrganizer,
        CancellationToken cancellationToken = default);

    Task DeleteByBoothIdAsync(
        Guid boothId,
        CancellationToken cancellationToken = default);

    Task<BoothOrganizer?> GetByOrganizerAndRaceAsync(
        Guid organizerId,
        Guid raceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BoothOrganizer>> GetByRaceIdAsync(
        Guid raceId,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedAsync(
        Guid organizerId,
        Guid boothId,
        CancellationToken cancellationToken = default);
}
