namespace OVCMOVE.Application.Features.Races.Query.ScoringLog;

public record ScoringLogResultModel
{
    public Guid LogId { get; init;}
    public string? BoothName {get; init;} // null if actor != organizer
    public string EventName {get; set;} = string.Empty;
    public string TeamName {get; init;}= string.Empty;
    public string? ActorFullName {get; init;}
    public string? ActorShortName {get; init;}
    public int ScoreDelta {get; init;}
    public int ScoreBefore {get; init;}
    public int ScoreAfter {get; init;}
    public string Reason {get; set;} = string.Empty;
    public DateTime CreatedAt {get; init;}
    public string CreatedBy {get; init;} = string.Empty;
}