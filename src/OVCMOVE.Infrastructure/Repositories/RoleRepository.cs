using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IDbExecutor _db;

    public RoleRepository(IDbExecutor db) =>
        _db = db;

    public async Task<IReadOnlyCollection<RoleSummaryModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = await _db.QueryAsync<RoleSummaryModel>(RbacQueries.GetAllRolesQuery(), cancellationToken: cancellationToken);
        return roles.ToArray();
    }

    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.QueryFirstOrDefaultAsync<Role>(RbacQueries.GetRoleByIdQuery(), new { RoleId = roleId }, cancellationToken: cancellationToken);
    }

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.QueryFirstOrDefaultAsync<Role>(RbacQueries.GetRoleByCodeQuery(), new { Code = code }, cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(RbacQueries.CreateRoleQuery(), role, cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(Role));
    }

    public async Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(RbacQueries.UpdateRoleQuery(), role, cancellationToken: cancellationToken);
        return affectedRows >= 1;
    }

    public async Task<bool> SoftDeleteAsync(Guid roleId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RbacQueries.SoftDeleteRoleQuery(),
            new { RoleId = roleId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
