using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Roles.Query.GetAllRoles;

public class GetAllRolesQueryHandler(
    IRoleRepository roleRepository,
    IRolePermissionRepository rolePermissionRepository)
    : IRequestHandler<GetAllRolesQuery, IReadOnlyCollection<RoleSummaryModel>>
{
    /// <summary>Returns all active RBAC roles together with their permission assignments.</summary>
    public async Task<IReadOnlyCollection<RoleSummaryModel>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetAllAsync(cancellationToken);
        var result = new List<RoleSummaryModel>(roles.Count);

        foreach (var role in roles)
        {
            var permissionIds = await rolePermissionRepository
                .GetPermissionIdsByRoleIdAsync(role.Id, cancellationToken);
            result.Add(new RoleSummaryModel
            {
                Id = role.Id,
                Name = role.Name,
                Code = role.Code,
                Description = role.Description,
                IsSystem = role.IsSystem,
                CreatedAt = role.CreatedAt,
                ModifiedAt = role.ModifiedAt,
                PermissionCount = role.PermissionCount,
                PermissionIds = permissionIds
            });
        }

        return result;
    }
}
