using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommandHandler(IRolePermissionRepository rolePermissionRepository)
    : IRequestHandler<RemovePermissionFromRoleCommand, bool>
{
    public Task<bool> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var actor = string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy.Trim();
        return rolePermissionRepository.SoftDeleteAsync(request.RoleId, request.PermissionId, actor, DateTime.UtcNow, cancellationToken);
    }
}
