using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IOrganizerRepository
{
    Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IEnumerable<Guid> organizerIds,
        CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid organizerId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<GetAllOrganizersResultModel> Items, int TotalItems)> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<User>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default);
    Task<bool> ChangeStatusAsync(Guid organizerId, string status, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(User organizer, CancellationToken cancellationToken = default);
}
