using MediatR;

namespace OVCMOVE.Application.Features.Booths.Commands.AcceptEntryToBooth;

public class AcceptEntryToBoothCommand : IRequest<(bool IsSuccess, string Message)>
{
    public Guid BoothId { get; set; }
    public Guid TeamId { get; set; }
    public Guid OrganizerId { get; set; }
}
