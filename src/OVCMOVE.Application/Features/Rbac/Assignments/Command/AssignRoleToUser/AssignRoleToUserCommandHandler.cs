using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignRoleToUser;

public class AssignRoleToUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository)
    : IRequestHandler<AssignRoleToUserCommand, UserRoleAssignmentModel?>
{
    /// <summary>Assigns one role to a user when both records exist.</summary>
    public async Task<UserRoleAssignmentModel?> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (user is null || role is null)
        {
            return null;
        }

        var currentRoleIds = await userRoleRepository.GetRoleIdsByUserIdAsync(request.UserId, cancellationToken);
        if (!currentRoleIds.Contains(request.RoleId))
        {
            var now = DateTime.UtcNow;
            var actor = request.GetActorOrSystem();
            await userRoleRepository.CreateAsync(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                RoleId = request.RoleId,
                CreatedAt = now,
                CreatedBy = actor,
                ModifiedAt = now,
                ModifiedBy = actor,
                IsDeleted = false
            }, cancellationToken);
        }

        return new UserRoleAssignmentModel
        {
            UserId = request.UserId,
            RoleId = request.RoleId
        };
    }
}
