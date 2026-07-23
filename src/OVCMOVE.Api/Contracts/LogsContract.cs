namespace OVCMOVE.Api.Contracts;

public class LogsContract
{
    public class BoothScoringLogsRequest
    {
        public int? Limit {get; set;}
    }
    public class BoothScoringLogsResponse
    {
        public string BoothName {get; set;}
        public string TeamName {get; set;}
        public string OrganizerName {get; set;}
        public int ScoreGiven {get; set;}
        public DateTime CreatedAt {get; set;}
    }

}