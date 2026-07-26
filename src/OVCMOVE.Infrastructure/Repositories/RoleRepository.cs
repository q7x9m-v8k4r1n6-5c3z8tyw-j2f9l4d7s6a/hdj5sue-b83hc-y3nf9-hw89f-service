using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class RoleRepository : BaseRepository<RoleRepository>, IRoleRepository
{
    public RoleRepository(ILogger<RoleRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<IReadOnlyCollection<RoleSummaryModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var roles = await _dapperHelper.QueryAsync<RoleSummaryModel>(RbacQueries.GetAllRolesQuery(), cancellationToken: cancellationToken);
        return roles.ToArray();
    }

    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.QueryFirstOrDefaultAsync<Role>(RbacQueries.GetRoleByIdQuery(), new { RoleId = roleId }, cancellationToken: cancellationToken);
    }

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.QueryFirstOrDefaultAsync<Role>(RbacQueries.GetRoleByCodeQuery(), new { Code = code }, cancellationToken: cancellationToken);
    }

    public async Task<Guid?> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RbacQueries.CreateRoleQuery(), role, cancellationToken: cancellationToken);
        return affectedRows >= 1 ? role.Id : null;
    }

    public async Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RbacQueries.UpdateRoleQuery(), role, cancellationToken: cancellationToken);
        return affectedRows >= 1;
    }

    public async Task<bool> SoftDeleteAsync(Guid roleId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(
            RbacQueries.SoftDeleteRoleQuery(),
            new { RoleId = roleId, ModifiedBy = modifiedBy, ModifiedAt = modifiedAt },
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }
}
