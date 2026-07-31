using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Command.DeleteTeam;

public sealed class DeleteTeamCommand : AuditedRequest, IRequest<bool>
{
    public Guid TeamId { get; init; }
}
