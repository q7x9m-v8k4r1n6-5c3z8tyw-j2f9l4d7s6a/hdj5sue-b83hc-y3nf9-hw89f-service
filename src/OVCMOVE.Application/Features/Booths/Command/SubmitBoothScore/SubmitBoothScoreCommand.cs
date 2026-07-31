using MediatR;

namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreCommand : IRequest<bool>
{
    public Guid BoothID { get; set; }
    public Guid TeamID { get; set; }
    public Guid OrganizerId { get; set; }
    public int Score { get; set; }
    public string EventCode { get; set; } = "BOOTH";
    public string EventName { get; set; } = "Chấm điểm trạm";
    public string ReasonCode { get; set; } = "BOOTH_COMPLETED";
    public string Reason { get; set; } = "Hoàn thành thử thách tại trạm";
}