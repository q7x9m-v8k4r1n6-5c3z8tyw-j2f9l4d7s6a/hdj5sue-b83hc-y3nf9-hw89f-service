using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceDetail;

public class GetRaceDetailQueryHandler :
    IRequestHandler<GetRaceDetailQuery, RaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;

    public GetRaceDetailQueryHandler(IRaceRepository raceRepository)
    {
        _raceRepository = raceRepository;
    }

    /// <summary>Returns the complete race view or null when the race is missing.</summary>
    public Task<RaceDetailResultModel?> Handle(GetRaceDetailQuery request, CancellationToken cancellationToken)
    {
        return _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
    }
}
