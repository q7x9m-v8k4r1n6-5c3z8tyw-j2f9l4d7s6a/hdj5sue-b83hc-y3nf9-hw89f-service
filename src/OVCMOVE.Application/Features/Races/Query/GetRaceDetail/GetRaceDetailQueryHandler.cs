using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceDetail;

public class GetRaceDetailQueryHandler :
    BaseQueryHandler<GetRaceDetailQueryHandler>,
    IRequestHandler<GetRaceDetailQuery, RaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;

    public GetRaceDetailQueryHandler(ILogger<GetRaceDetailQueryHandler> logger, IRaceRepository raceRepository) : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public Task<RaceDetailResultModel?> Handle(GetRaceDetailQuery request, CancellationToken cancellationToken)
    {
        return _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
    }
}
