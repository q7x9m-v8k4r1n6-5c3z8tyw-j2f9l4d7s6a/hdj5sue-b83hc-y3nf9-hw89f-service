using MediatR;

namespace OVCMOVE.Application.Features.Teams.Query.GetMySession;

public sealed record GetMySessionQuery(
    Guid RaceId,
    Guid TeamId) : IRequest<MySessionResultModel?>;
