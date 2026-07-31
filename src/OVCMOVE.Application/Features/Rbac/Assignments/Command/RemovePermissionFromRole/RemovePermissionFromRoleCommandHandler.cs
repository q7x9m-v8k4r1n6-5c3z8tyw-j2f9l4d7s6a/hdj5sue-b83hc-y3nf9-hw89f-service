using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommandHandler(
    IRoleRepository roleRepository,
    IRolePermissionRepository rolePermissionRepository)
    : IRequestHandler<RemovePermissionFromRoleCommand, bool>
{
    /// <summary>Removes one permission assignment from a role.</summary>
    public async Task<bool> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        var actor = request.GetActorOrSystem();
        var modifiedAt = DateTime.UtcNow;
        var removed = await rolePermissionRepository.SoftDeleteAsync(
            request.RoleId,
            request.PermissionId,
            actor,
            modifiedAt,
            cancellationToken);
        if (!removed)
        {
            return false;
        }

        role.ModifiedAt = modifiedAt;
        role.ModifiedBy = actor;
        await roleRepository.UpdateAsync(role, cancellationToken);
        return true;
    }
}
