using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Query.GetAllPermissions;

public class GetAllPermissionsQueryHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<GetAllPermissionsQuery, IReadOnlyCollection<PermissionSummaryModel>>
{
    public Task<IReadOnlyCollection<PermissionSummaryModel>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        return permissionRepository.GetAllAsync(cancellationToken);
    }
}
