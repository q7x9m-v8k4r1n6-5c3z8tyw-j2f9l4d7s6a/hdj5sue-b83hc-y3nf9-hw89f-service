using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class RolePermissionRepository : BaseRepository<RolePermissionRepository>, IRolePermissionRepository
{
    public RolePermissionRepository(ILogger<RolePermissionRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<IReadOnlyCollection<Guid>> GetPermissionIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permissionIds = await _dapperHelper.QueryAsync<Guid>(RbacQueries.GetPermissionIdsByRoleIdQuery(), new { RoleId = roleId }, cancellationToken: cancellationToken);
        return permissionIds.ToArray();
    }

    public async Task<Guid?> CreateAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RbacQueries.CreateRolePermissionQuery(), rolePermission, cancellationToken: cancellationToken);
        return affectedRows >= 1 ? rolePermission.Id : null;
    }

    public async Task<bool> SoftDeleteAsync(Guid roleId, Guid permissionId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(
            RbacQueries.SoftDeleteRolePermissionQuery(),
            new { RoleId = roleId, PermissionId = permissionId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
