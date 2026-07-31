using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public sealed class CreateTeamCommand : AuditedRequest, IRequest<CreateTeamResponse>
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
