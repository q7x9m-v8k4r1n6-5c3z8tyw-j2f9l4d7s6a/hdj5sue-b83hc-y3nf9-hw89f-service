using MediatR;

namespace OVCMOVE.Application.Features.Booths.Commands.RejectEntryToBooth;

public sealed record RejectEntryToBoothCommand(
    Guid BoothId,
    Guid TeamId,
    Guid OrganizerId) : IRequest;
