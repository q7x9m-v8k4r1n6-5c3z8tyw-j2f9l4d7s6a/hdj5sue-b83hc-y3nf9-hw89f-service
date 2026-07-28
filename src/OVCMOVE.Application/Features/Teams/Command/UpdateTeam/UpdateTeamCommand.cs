using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Command.UpdateTeam;

public sealed class UpdateTeamCommand : AuditedRequest, IRequest<bool>
{
    public Guid TeamId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool ResetPassword { get; init; }
    public string Status { get; init; } = string.Empty;
}
