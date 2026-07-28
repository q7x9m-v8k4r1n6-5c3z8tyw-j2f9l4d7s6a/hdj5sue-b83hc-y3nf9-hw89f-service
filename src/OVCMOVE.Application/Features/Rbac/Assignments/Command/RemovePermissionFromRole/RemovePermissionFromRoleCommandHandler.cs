using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommandHandler(IRolePermissionRepository rolePermissionRepository)
    : IRequestHandler<RemovePermissionFromRoleCommand, bool>
{
    /// <summary>Removes one permission assignment from a role.</summary>
    public Task<bool> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var actor = request.GetActorOrSystem();
        return rolePermissionRepository.SoftDeleteAsync(request.RoleId, request.PermissionId, actor, DateTime.UtcNow, cancellationToken);
    }
}
