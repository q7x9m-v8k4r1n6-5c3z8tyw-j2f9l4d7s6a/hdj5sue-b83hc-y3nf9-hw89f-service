using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly IDbExecutor _db;

    public UserRoleRepository(IDbExecutor db) =>
        _db = db;

    public async Task<IReadOnlyCollection<Guid>> GetRoleIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roleIds = await _db.QueryAsync<Guid>(RbacQueries.GetRoleIdsByUserIdQuery(), new { UserId = userId }, cancellationToken: cancellationToken);
        return roleIds.ToArray();
    }

    public async Task CreateAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(RbacQueries.CreateUserRoleQuery(), userRole, cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(
            affectedRows,
            nameof(UserRole));
    }

    public async Task<bool> SoftDeleteAsync(Guid userId, Guid roleId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RbacQueries.SoftDeleteUserRoleQuery(),
            new { UserId = userId, RoleId = roleId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
