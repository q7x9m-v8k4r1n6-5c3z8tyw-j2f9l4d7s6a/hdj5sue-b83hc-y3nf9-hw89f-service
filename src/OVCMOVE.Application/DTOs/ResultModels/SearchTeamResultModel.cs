namespace OVCMOVE.Application.Features.Teams.Query.SearchTeam;

public class SearchTeamResultModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LeaderEmail { get; set; } = string.Empty;
}