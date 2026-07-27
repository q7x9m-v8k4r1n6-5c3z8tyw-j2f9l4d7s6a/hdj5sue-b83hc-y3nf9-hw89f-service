using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface ITeamRepository
{
    Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IEnumerable<Guid> teamIds,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<User> Items, int TotalItems)> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<User>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default);
}
