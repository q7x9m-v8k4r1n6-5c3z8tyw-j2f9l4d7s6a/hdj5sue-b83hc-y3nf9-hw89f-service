using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRolePermissionRepository
{
    Task<IReadOnlyCollection<Guid>> GetPermissionIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task CreateAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid roleId, Guid permissionId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default);
}
