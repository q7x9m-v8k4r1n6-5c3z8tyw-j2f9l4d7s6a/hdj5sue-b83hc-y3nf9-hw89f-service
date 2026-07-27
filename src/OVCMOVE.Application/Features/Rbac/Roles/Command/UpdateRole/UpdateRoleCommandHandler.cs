using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Application.Features.Rbac;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.UpdateRole;

public class UpdateRoleCommandHandler(
    IRoleRepository roleRepository)
    : IRequestHandler<UpdateRoleCommand, RoleSummaryModel?>
{
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>Updates an existing RBAC role when its code remains unique.</summary>
    public async Task<RoleSummaryModel?> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return null;
        }

        if (role.IsSystem)
        {
            throw new ApplicationConflictException(
                "Không thể chỉnh sửa role hệ thống.");
        }

        var name = RbacInput.Required(request.Name, "Tên role", 100);
        var normalizedCode = RbacInput.Code(
            request.Code,
            "Role code",
            100);
        var description = RbacInput.Optional(
            request.Description,
            "Mô tả role",
            500);
        var existing = await _roleRepository.GetByCodeAsync(normalizedCode, cancellationToken);
        if (existing is not null && existing.Id != role.Id)
        {
            throw new ApplicationConflictException(
                $"Role code '{normalizedCode}' đã tồn tại.");
        }

        role.Name = name;
        role.Code = normalizedCode;
        role.Description = description;
        role.ModifiedAt = DateTime.UtcNow;
        role.ModifiedBy = request.GetActorOrSystem();

        var updated = await _roleRepository.UpdateAsync(role, cancellationToken);
        if (!updated)
        {
            return null;
        }

        return new RoleSummaryModel
        {
            Id = role.Id,
            Name = role.Name,
            Code = role.Code,
            Description = role.Description,
            IsSystem = role.IsSystem,
            CreatedAt = role.CreatedAt
        };
    }
}
