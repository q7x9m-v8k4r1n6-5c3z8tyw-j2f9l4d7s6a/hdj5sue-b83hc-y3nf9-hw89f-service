namespace OVCMOVE.Application.DTOs.Team;

public class UpdateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
