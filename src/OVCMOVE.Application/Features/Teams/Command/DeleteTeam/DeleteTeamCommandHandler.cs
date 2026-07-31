using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Teams.Command.DeleteTeam;

public sealed class DeleteTeamCommandHandler(IUserRepository users)
    : IRequestHandler<DeleteTeamCommand, bool>
{
    public async Task<bool> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var team = await users.GetByIdAsync(request.TeamId, cancellationToken);
        if (team?.UserType != UserConstants.UserType.Team)
        {
            return false;
        }

        return await users.SoftDeleteAsync(
            request.TeamId,
            UserConstants.UserType.Team,
            request.GetActorOrSystem(),
            DateTime.UtcNow,
            cancellationToken);
    }
}
