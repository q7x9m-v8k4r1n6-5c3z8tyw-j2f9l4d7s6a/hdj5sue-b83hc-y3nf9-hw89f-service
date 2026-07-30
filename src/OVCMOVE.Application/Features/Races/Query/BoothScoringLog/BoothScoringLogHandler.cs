using MediatR;

using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Races.Query.BoothScoringLog;

public class BoothScoringLogQueryHandler : 
    IRequestHandler<BoothScoringLogQuery, List<BoothScoringLogResultModel>>
{
    private readonly IRaceRepository _raceRepository;

    public BoothScoringLogQueryHandler(
        IRaceRepository raceRepository) 
    {
        _raceRepository = raceRepository;
    }

    public async Task<List<BoothScoringLogResultModel>> Handle(
        BoothScoringLogQuery request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _raceRepository.GetBoothScoringLogAsync(request.RaceId, request.Limit, cancellationToken);
    }
}