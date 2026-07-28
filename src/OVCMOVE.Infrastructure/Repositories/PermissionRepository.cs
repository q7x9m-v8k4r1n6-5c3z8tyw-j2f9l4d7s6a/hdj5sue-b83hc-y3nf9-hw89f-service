using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly IDbExecutor _db;

    public PermissionRepository(IDbExecutor db) =>
        _db = db;

    public async Task<IReadOnlyCollection<PermissionSummaryModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permissions = await _db.QueryAsync<PermissionSummaryModel>(RbacQueries.GetAllPermissionsQuery(), cancellationToken: cancellationToken);
        return permissions.ToArray();
    }

    public Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.QueryFirstOrDefaultAsync<Permission>(RbacQueries.GetPermissionByIdQuery(), new { PermissionId = permissionId }, cancellationToken: cancellationToken);
    }

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.QueryFirstOrDefaultAsync<Permission>(RbacQueries.GetPermissionByCodeQuery(), new { Code = code }, cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(RbacQueries.CreatePermissionQuery(), permission, cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(
            affectedRows,
            nameof(Permission));
    }

    public async Task<bool> UpdateAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(RbacQueries.UpdatePermissionQuery(), permission, cancellationToken: cancellationToken);
        return affectedRows >= 1;
    }

    public async Task<bool> SoftDeleteAsync(Guid permissionId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RbacQueries.SoftDeletePermissionQuery(),
            new { PermissionId = permissionId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
