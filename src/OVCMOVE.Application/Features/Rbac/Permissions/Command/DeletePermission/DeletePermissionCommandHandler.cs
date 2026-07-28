using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.DeletePermission;

public class DeletePermissionCommandHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<DeletePermissionCommand, bool>
{
    /// <summary>Soft-deletes a non-system RBAC permission.</summary>
    public async Task<bool> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
        {
            return false;
        }

        if (permission.IsSystem)
        {
            throw new ApplicationConflictException(
                "Không thể xóa permission hệ thống.");
        }

        var actor = request.GetActorOrSystem();
        return await permissionRepository.SoftDeleteAsync(request.PermissionId, actor, DateTime.UtcNow, cancellationToken);
    }
}
