using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandler(IUserRoleRepository userRoleRepository)
    : IRequestHandler<RemoveRoleFromUserCommand, bool>
{
    public Task<bool> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var actor = string.IsNullOrWhiteSpace(request.ModifiedBy) ? "system" : request.ModifiedBy.Trim();
        return userRoleRepository.SoftDeleteAsync(request.UserId, request.RoleId, actor, DateTime.UtcNow, cancellationToken);
    }
}
