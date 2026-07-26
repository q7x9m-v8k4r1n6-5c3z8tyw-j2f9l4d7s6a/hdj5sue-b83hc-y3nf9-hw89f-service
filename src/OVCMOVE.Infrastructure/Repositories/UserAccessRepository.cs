using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class UserAccessRepository : BaseRepository<UserAccessRepository>, IUserAccessRepository
{
    public UserAccessRepository(ILogger<UserAccessRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<UserAccessProfileModel> GetAccessProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = (await _dapperHelper.QueryAsync<RoleAccessModel>(
            RbacQueries.GetAccessRolesByUserIdQuery(),
            new { UserId = userId },
            cancellationToken: cancellationToken)).ToArray();

        var permissions = (await _dapperHelper.QueryAsync<PermissionAccessModel>(
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
