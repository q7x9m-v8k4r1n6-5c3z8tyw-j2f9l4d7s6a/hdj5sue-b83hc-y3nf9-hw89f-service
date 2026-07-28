using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IDbExecutor _db;

    public RolePermissionRepository(IDbExecutor db) =>
        _db = db;

    public async Task<IReadOnlyCollection<Guid>> GetPermissionIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permissionIds = await _db.QueryAsync<Guid>(RbacQueries.GetPermissionIdsByRoleIdQuery(), new { RoleId = roleId }, cancellationToken: cancellationToken);
        return permissionIds.ToArray();
    }

    public async Task CreateAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(RbacQueries.CreateRolePermissionQuery(), rolePermission, cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(
            affectedRows,
            nameof(RolePermission));
    }

    public async Task<bool> SoftDeleteAsync(Guid roleId, Guid permissionId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RbacQueries.SoftDeleteRolePermissionQuery(),
            new { RoleId = roleId, PermissionId = permissionId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
