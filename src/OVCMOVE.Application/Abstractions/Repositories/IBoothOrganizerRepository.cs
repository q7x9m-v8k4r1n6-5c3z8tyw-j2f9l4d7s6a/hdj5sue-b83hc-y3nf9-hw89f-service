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

    Task<BoothOrganizer?> GetByOrganizerIdAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default);
}
