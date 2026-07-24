using MediatR;
using OVCMOVE.Application.DTOs.Team;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public class CreateTeamCommand : IRequest<TeamResponse>
{
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
}
