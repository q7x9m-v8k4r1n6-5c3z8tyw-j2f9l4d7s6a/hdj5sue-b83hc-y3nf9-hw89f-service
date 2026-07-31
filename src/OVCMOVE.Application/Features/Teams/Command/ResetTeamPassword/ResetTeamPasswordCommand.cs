using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Command.ResetTeamPassword;

public sealed class ResetTeamPasswordCommand : AuditedRequest, IRequest<bool>
{
    public Guid TeamId { get; init; }
}
