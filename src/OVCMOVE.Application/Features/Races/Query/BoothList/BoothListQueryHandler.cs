using MediatR;

using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Races.Query.BoothList;

public class BoothListQueryHandler : 
    IRequestHandler<BoothListQuery, List<BoothListResultModel>>
{
    private readonly IRaceRepository _raceRepository;

    public BoothListQueryHandler(
        IRaceRepository raceRepository) 
    {
        _raceRepository = raceRepository;
    }

    public async Task<List<BoothListResultModel>> Handle(
        BoothListQuery request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _raceRepository.GetBoothListAsync(request.RaceId, cancellationToken);
    }
}