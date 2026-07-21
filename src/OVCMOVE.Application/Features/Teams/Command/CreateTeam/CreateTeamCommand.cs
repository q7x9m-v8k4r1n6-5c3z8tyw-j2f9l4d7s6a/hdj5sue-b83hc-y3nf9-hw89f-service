using MediatR;
using OVCMOVE.Application.DTOs.Team;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public class CreateTeamCommand : IRequest<TeamResponse>
{
    public string Name { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
