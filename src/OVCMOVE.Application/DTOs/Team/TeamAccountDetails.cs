namespace OVCMOVE.Application.DTOs.Team;

// Read model for the Teams-to-Users join. These fields are not columns of dbo.Teams.
public class TeamAccountDetails
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
