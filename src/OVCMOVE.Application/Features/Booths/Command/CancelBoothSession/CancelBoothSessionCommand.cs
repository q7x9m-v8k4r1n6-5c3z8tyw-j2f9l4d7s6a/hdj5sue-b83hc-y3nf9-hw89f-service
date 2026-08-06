using MediatR;

namespace OVCMOVE.Application.Features.Booths.Commands.CancelBoothSession;

public sealed record CancelBoothSessionCommand(
    Guid BoothId,
    Guid OrganizerId) : IRequest;
