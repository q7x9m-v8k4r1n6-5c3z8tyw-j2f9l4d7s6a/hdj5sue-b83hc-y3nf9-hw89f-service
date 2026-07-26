using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class UserRoleRepository : BaseRepository<UserRoleRepository>, IUserRoleRepository
{
    public UserRoleRepository(ILogger<UserRoleRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<IReadOnlyCollection<Guid>> GetRoleIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roleIds = await _dapperHelper.QueryAsync<Guid>(RbacQueries.GetRoleIdsByUserIdQuery(), new { UserId = userId }, cancellationToken: cancellationToken);
        return roleIds.ToArray();
    }

    public async Task<Guid?> CreateAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RbacQueries.CreateUserRoleQuery(), userRole, cancellationToken: cancellationToken);
        return affectedRows >= 1 ? userRole.Id : null;
    }

    public async Task<bool> SoftDeleteAsync(Guid userId, Guid roleId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(
            RbacQueries.SoftDeleteUserRoleQuery(),
            new { UserId = userId, RoleId = roleId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
