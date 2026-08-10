using MediatR;
using OVCMOVE.Application.Features.Races.Common;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceMessages;

public sealed class GetRaceMessagesQuery : IRequest<IReadOnlyCollection<RaceMessageResultModel>>
{
    public Guid RaceId { get; init; }
    public int Limit { get; init; } = 50;
}
