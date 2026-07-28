namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

public sealed class CreateTeamResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
}
