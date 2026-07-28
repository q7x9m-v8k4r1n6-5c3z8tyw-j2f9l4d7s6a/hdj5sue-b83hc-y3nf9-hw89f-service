using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Features.Rbac;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.CreatePermission;

public class CreatePermissionCommandHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<CreatePermissionCommand, PermissionSummaryModel>
{
    /// <summary>Creates a unique RBAC permission.</summary>
    public async Task<PermissionSummaryModel> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
        if (existing is not null)
        {
            throw new ApplicationConflictException(
                $"Permission code '{code}' đã tồn tại.");
        }

        var now = DateTime.UtcNow;
        var actor = request.GetActorOrSystem();
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Description = description,
            Module = module,
            Action = action,
            IsSystem = false,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor,
            IsDeleted = false
        };

        await permissionRepository.CreateAsync(permission, cancellationToken);

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
