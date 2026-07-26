using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.DeletePermission;

public class DeletePermissionCommandHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<DeletePermissionCommand, bool>
{
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
            throw new InvalidOperationException("System permissions cannot be deleted.");
        }

        var actor = string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy.Trim();
        return await permissionRepository.SoftDeleteAsync(request.PermissionId, actor, DateTime.UtcNow, cancellationToken);
    }
}
