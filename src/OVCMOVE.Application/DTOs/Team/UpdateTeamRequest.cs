namespace OVCMOVE.Application.DTOs.Team;

public class UpdateTeamRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
}
