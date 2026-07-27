using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Query.GetAllPermissions;

public class GetAllPermissionsQueryHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<GetAllPermissionsQuery, IReadOnlyCollection<PermissionSummaryModel>>
{
    /// <summary>Returns all active RBAC permissions.</summary>
    public Task<IReadOnlyCollection<PermissionSummaryModel>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        return permissionRepository.GetAllAsync(cancellationToken);
    }
}
