using MediatR;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public class CreateTeamCommand : IRequest<bool>
{
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
