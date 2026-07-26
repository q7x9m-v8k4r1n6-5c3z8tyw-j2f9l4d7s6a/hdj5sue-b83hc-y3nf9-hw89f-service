using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.CreatePermission;

public class CreatePermissionCommandHandler(IPermissionRepository permissionRepository)
    : IRequestHandler<CreatePermissionCommand, PermissionSummaryModel>
{
    public async Task<PermissionSummaryModel> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var code = request.Code.Trim();
        var existing = await permissionRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Permission code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var actor = string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy.Trim();
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim(),
            Module = request.Module.Trim(),
            Action = request.Action.Trim(),
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
