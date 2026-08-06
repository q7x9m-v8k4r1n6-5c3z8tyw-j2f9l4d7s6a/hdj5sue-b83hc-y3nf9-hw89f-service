namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreModel
{
    public Guid BoothId { get; set; }
    public Guid TeamId { get; set; }
    public Guid OrganizerId { get; set; }
    public int Score { get; set; }
    public string EventCode { get; set; } =
        OVCMOVE.Domain.Constants.ScoringLogConstants.EventCode.Booth;
    public string EventName { get; set; } =
        OVCMOVE.Domain.Constants.ScoringLogConstants.EventName.BoothScoring;
    public string ReasonCode { get; set; } =
        OVCMOVE.Domain.Constants.ScoringLogConstants.ReasonCode.BoothCompleted;
    public string Reason { get; set; } =
        OVCMOVE.Domain.Constants.ScoringLogConstants.Reason.BoothCompleted;
}
