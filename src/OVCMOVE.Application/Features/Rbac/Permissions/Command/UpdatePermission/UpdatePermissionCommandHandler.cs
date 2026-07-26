using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.UpdatePermission;

public class UpdatePermissionCommandHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<UpdatePermissionCommand, PermissionSummaryModel?>
{
    public async Task<PermissionSummaryModel?> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
        {
            return null;
        }

        var code = request.Code.Trim();
        var existing = await permissionRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != permission.Id)
        {
            throw new InvalidOperationException($"Permission code '{code}' already exists.");
        }

        permission.Name = request.Name.Trim();
        permission.Code = code;
        permission.Description = request.Description?.Trim();
        permission.Module = request.Module.Trim();
        permission.Action = request.Action.Trim();
        permission.ModifiedAt = DateTime.UtcNow;
        permission.ModifiedBy = string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy.Trim();

        var updated = await permissionRepository.UpdateAsync(permission, cancellationToken);
        if (!updated)
        {
            return null;
        }

        return new PermissionSummaryModel
        {
            Id = permission.Id,
            Name = permission.Name,
            Code = permission.Code,
            Description = permission.Description,
            Module = permission.Module,
            Action = permission.Action,
            IsSystem = permission.IsSystem
        };
    }
}
