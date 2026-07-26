using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.CreateRole;

public class CreateRoleCommandHandler(IRoleRepository roleRepository)
    : IRequestHandler<CreateRoleCommand, RoleSummaryModel>
{
    public async Task<RoleSummaryModel> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var code = NormalizeCode(request.Code);
        var existing = await roleRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Role code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var actor = ResolveActor(request.ModifiedBy);
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description?.Trim(),
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

    private static string NormalizeCode(string code) => code.Trim().ToLowerInvariant();

    private static string ResolveActor(string? modifiedBy) => string.IsNullOrWhiteSpace(modifiedBy) ? "system" : modifiedBy.Trim();
}
