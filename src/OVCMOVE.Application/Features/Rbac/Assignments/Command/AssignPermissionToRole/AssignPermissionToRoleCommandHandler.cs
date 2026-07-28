using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignPermissionToRole;

public class AssignPermissionToRoleCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IRolePermissionRepository rolePermissionRepository)
    : IRequestHandler<AssignPermissionToRoleCommand, RolePermissionAssignmentModel?>
{
    /// <summary>Assigns one permission to a role when both records exist.</summary>
    public async Task<RolePermissionAssignmentModel?> Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (role is null || permission is null)
        {
            return null;
        }

        var currentPermissionIds = await rolePermissionRepository.GetPermissionIdsByRoleIdAsync(request.RoleId, cancellationToken);
        if (!currentPermissionIds.Contains(request.PermissionId))
        {
            var now = DateTime.UtcNow;
            var actor = request.GetActorOrSystem();
            await rolePermissionRepository.CreateAsync(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = request.RoleId,
                PermissionId = request.PermissionId,
                CreatedAt = now,
                CreatedBy = actor,
                ModifiedAt = now,
                ModifiedBy = actor,
                IsDeleted = false
            }, cancellationToken);

            role.ModifiedAt = now;
            role.ModifiedBy = actor;
            await roleRepository.UpdateAsync(role, cancellationToken);
        }

        return new RolePermissionAssignmentModel
        {
            RoleId = request.RoleId,
            PermissionId = request.PermissionId
        };
    }
}
