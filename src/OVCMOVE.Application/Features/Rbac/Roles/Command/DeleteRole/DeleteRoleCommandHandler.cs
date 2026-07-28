using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.DeleteRole;

public class DeleteRoleCommandHandler(IRoleRepository roleRepository) : IRequestHandler<DeleteRoleCommand, bool>
{
    /// <summary>Soft-deletes a non-system RBAC role.</summary>
    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        if (role.IsSystem)
        {
            throw new ApplicationConflictException(
                "Không thể xóa role hệ thống.");
        }

        var actor = request.GetActorOrSystem();
        return await roleRepository.SoftDeleteAsync(request.RoleId, actor, DateTime.UtcNow, cancellationToken);
    }
}
