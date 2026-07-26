using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Roles.Query.GetAllRoles;

public class GetAllRolesQueryHandler(
    ILogger<GetAllRolesQueryHandler> logger,
    IRoleRepository roleRepository)
    : BaseQueryHandler<GetAllRolesQueryHandler>(logger),
      IRequestHandler<GetAllRolesQuery, IReadOnlyCollection<RoleSummaryModel>>
{
    public Task<IReadOnlyCollection<RoleSummaryModel>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        return roleRepository.GetAllAsync(cancellationToken);
    }
}
