using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandler(IUserRoleRepository userRoleRepository)
    : IRequestHandler<RemoveRoleFromUserCommand, bool>
{
    /// <summary>Removes one role assignment from a user.</summary>
    public Task<bool> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var actor = request.GetActorOrSystem();
        return userRoleRepository.SoftDeleteAsync(request.UserId, request.RoleId, actor, DateTime.UtcNow, cancellationToken);
    }
}
