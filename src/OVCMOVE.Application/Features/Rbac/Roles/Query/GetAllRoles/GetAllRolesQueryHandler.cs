using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Roles.Query.GetAllRoles;

public class GetAllRolesQueryHandler(
    IRoleRepository roleRepository)
    : IRequestHandler<GetAllRolesQuery, IReadOnlyCollection<RoleSummaryModel>>
{
    /// <summary>Returns all active RBAC roles.</summary>
    public Task<IReadOnlyCollection<RoleSummaryModel>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        return roleRepository.GetAllAsync(cancellationToken);
    }
}
