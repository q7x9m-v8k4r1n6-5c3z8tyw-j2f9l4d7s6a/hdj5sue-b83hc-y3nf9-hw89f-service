using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.DeleteRole;

public class DeleteRoleCommandHandler(IRoleRepository roleRepository) : IRequestHandler<DeleteRoleCommand, bool>
{
    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        if (role.IsSystem)
        {
            throw new InvalidOperationException("System roles cannot be deleted.");
        }

        var actor = string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy.Trim();
        return await roleRepository.SoftDeleteAsync(request.RoleId, actor, DateTime.UtcNow, cancellationToken);
    }
}
