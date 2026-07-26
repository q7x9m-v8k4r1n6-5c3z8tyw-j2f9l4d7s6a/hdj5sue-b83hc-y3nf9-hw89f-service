using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.UpdateRole;

public class UpdateRoleCommandHandler(
    ILogger<UpdateRoleCommandHandler> logger,
    IRoleRepository roleRepository)
    : IRequestHandler<UpdateRoleCommand, RoleSummaryModel?>
{
    private readonly ILogger<UpdateRoleCommandHandler> _logger = logger;
    private readonly IRoleRepository _roleRepository = roleRepository;

    public async Task<RoleSummaryModel?> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var normalizedCode = request.Code.Trim().ToLowerInvariant();
        var existing = await _roleRepository.GetByCodeAsync(normalizedCode, cancellationToken);
        if (existing is not null && existing.Id != role.Id)
        {
            throw new InvalidOperationException($"Role code '{normalizedCode}' already exists.");
        }

        role.Name = request.Name.Trim();
        role.Code = normalizedCode;
        role.Description = request.Description?.Trim();
        role.ModifiedAt = DateTime.UtcNow;
        role.ModifiedBy = string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy.Trim();

        var updated = await _roleRepository.UpdateAsync(role, cancellationToken);
        if (!updated)
        {
            _logger.LogWarning("Role {RoleId} could not be updated.", request.RoleId);
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
