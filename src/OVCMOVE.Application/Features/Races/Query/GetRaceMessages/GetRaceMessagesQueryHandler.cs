using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Common;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceMessages;

public sealed class GetRaceMessagesQueryHandler :
    IRequestHandler<GetRaceMessagesQuery, IReadOnlyCollection<RaceMessageResultModel>>
{
    private readonly IRaceRepository _raceRepository;

    public GetRaceMessagesQueryHandler(IRaceRepository raceRepository)
    {
        _raceRepository = raceRepository;
    }

    public async Task<IReadOnlyCollection<RaceMessageResultModel>> Handle(
        GetRaceMessagesQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.RaceId == Guid.Empty)
        {
            throw new ApplicationValidationException("RaceId is required.");
        }

        var limit = Math.Clamp(request.Limit, 1, 100);
        return await _raceRepository.GetRaceMessagesAsync(
            request.RaceId,
            limit,
            cancellationToken);
    }
}
