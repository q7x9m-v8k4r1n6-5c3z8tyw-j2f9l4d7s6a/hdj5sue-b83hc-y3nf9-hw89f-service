using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class UserAccessRepository : IUserAccessRepository
{
    private readonly IDbExecutor _db;

    public UserAccessRepository(IDbExecutor db) =>
        _db = db;

    public async Task<UserAccessProfileModel> GetAccessProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = (await _db.QueryAsync<RoleAccessModel>(
            RbacQueries.GetAccessRolesByUserIdQuery(),
            new { UserId = userId },
            cancellationToken: cancellationToken)).ToArray();

        var permissions = (await _db.QueryAsync<PermissionAccessModel>(
            RbacQueries.GetAccessPermissionsByUserIdQuery(),
            new { UserId = userId },
            cancellationToken: cancellationToken)).ToArray();

        return new UserAccessProfileModel
        {
            Roles = roles,
            Permissions = permissions,
            Access = permissions.Select(permission => permission.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }
}
