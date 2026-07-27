using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Application.Features.Rbac;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.UpdatePermission;

public class UpdatePermissionCommandHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<UpdatePermissionCommand, PermissionSummaryModel?>
{
    /// <summary>Updates an existing RBAC permission when its code remains unique.</summary>
    public async Task<PermissionSummaryModel?> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
        {
            return null;
        }

        if (permission.IsSystem)
        {
            throw new ApplicationConflictException(
                "Không thể chỉnh sửa permission hệ thống.");
        }

        var name = RbacInput.Required(
            request.Name,
            "Tên permission",
            150);
        var code = RbacInput.Code(
            request.Code,
            "Permission code",
            150);
        var module = RbacInput.Required(request.Module, "Module", 100);
        var action = RbacInput.Required(request.Action, "Action", 100);
        var description = RbacInput.Optional(
            request.Description,
            "Mô tả permission",
            500);
        var existing = await permissionRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null && existing.Id != permission.Id)
        {
            throw new ApplicationConflictException(
                $"Permission code '{code}' đã tồn tại.");
        }

        permission.Name = name;
        permission.Code = code;
        permission.Description = description;
        permission.Module = module;
        permission.Action = action;
        permission.ModifiedAt = DateTime.UtcNow;
        permission.ModifiedBy = request.GetActorOrSystem();

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
