using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Features.Rbac;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.CreateRole;

public class CreateRoleCommandHandler(IRoleRepository roleRepository)
    : IRequestHandler<CreateRoleCommand, RoleSummaryModel>
{
    /// <summary>Creates a unique RBAC role.</summary>
    public async Task<RoleSummaryModel> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = RbacInput.Required(request.Name, "Tên role", 100);
        var code = RbacInput.Code(request.Code, "Role code", 100);
        var description = RbacInput.Optional(
            request.Description,
            "Mô tả role",
            500);
        var existing = await roleRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new ApplicationConflictException(
                $"Role code '{code}' đã tồn tại.");
        }

        var now = DateTime.UtcNow;
        var actor = request.GetActorOrSystem();
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Description = description,
            IsSystem = false,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor,
            IsDeleted = false
        };

        await roleRepository.CreateAsync(role, cancellationToken);

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
