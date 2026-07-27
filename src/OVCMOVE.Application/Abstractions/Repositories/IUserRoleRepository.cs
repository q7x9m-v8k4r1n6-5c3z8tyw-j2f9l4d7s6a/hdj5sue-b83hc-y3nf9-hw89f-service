using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IUserRoleRepository
{
    Task<IReadOnlyCollection<Guid>> GetRoleIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CreateAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid userId, Guid roleId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default);
}
