namespace OVCMOVE.Application.Features.Races.Query.BoothScoringLog;

public record BoothScoringLogResultModel
{
    public Guid LogId { get; init;}
    public string BoothName {get; init;} = string.Empty;
    public string TeamName {get; init;}= string.Empty;
    public string OrganizerName {get; init;} = string.Empty;
    public int ScoreGiven {get; init;}
    public DateTime CreatedAt {get; init;}
    public string CreatedBy {get; init;} = string.Empty;
}