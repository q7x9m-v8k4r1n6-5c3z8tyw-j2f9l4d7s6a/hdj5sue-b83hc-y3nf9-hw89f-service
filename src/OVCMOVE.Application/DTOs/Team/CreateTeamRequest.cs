namespace OVCMOVE.Application.DTOs.Team;

public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
