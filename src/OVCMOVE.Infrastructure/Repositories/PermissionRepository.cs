using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class PermissionRepository : BaseRepository<PermissionRepository>, IPermissionRepository
{
    public PermissionRepository(ILogger<PermissionRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<IReadOnlyCollection<PermissionSummaryModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permissions = await _dapperHelper.QueryAsync<PermissionSummaryModel>(RbacQueries.GetAllPermissionsQuery(), cancellationToken: cancellationToken);
        return permissions.ToArray();
    }

    public Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.QueryFirstOrDefaultAsync<Permission>(RbacQueries.GetPermissionByIdQuery(), new { PermissionId = permissionId }, cancellationToken: cancellationToken);
    }

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.QueryFirstOrDefaultAsync<Permission>(RbacQueries.GetPermissionByCodeQuery(), new { Code = code }, cancellationToken: cancellationToken);
    }

    public async Task<Guid?> CreateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RbacQueries.CreatePermissionQuery(), permission, cancellationToken: cancellationToken);
        return affectedRows >= 1 ? permission.Id : null;
    }

    public async Task<bool> UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RbacQueries.UpdatePermissionQuery(), permission, cancellationToken: cancellationToken);
        return affectedRows >= 1;
    }

    public async Task<bool> SoftDeleteAsync(Guid permissionId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(
            RbacQueries.SoftDeletePermissionQuery(),
            new { PermissionId = permissionId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
