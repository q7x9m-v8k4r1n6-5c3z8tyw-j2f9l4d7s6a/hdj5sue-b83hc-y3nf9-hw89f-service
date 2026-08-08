using MediatR;

namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

using OVCMOVE.Domain.Constants;

public class SubmitBoothScoreCommand : IRequest<bool>
{
    public Guid BoothID { get; set; }
    public Guid TeamID { get; set; }
    public Guid OrganizerId { get; set; }
    public int Score { get; set; }
    public string EventCode { get; set; } =
        ScoringLogConstants.EventCode.Booth;
    public string EventName { get; set; } =
        ScoringLogConstants.EventName.BoothScoring;
    public string ReasonCode { get; set; } =
        ScoringLogConstants.ReasonCode.BoothCompleted;
    public string Reason { get; set; } =
        ScoringLogConstants.Reason.BoothCompleted;
}
