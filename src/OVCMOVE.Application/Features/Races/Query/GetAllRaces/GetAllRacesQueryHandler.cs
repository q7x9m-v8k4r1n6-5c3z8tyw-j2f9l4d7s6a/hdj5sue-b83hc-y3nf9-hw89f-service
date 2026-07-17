using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetAllRaces;

public class GetAllRacesQueryHandler :
    BaseQueryHandler<GetAllRacesQueryHandler>,
    IRequestHandler<GetAllRacesQuery, IReadOnlyCollection<RaceListItemResultModel>>
{
    private readonly IRaceRepository _raceRepository;

    public GetAllRacesQueryHandler(ILogger<GetAllRacesQueryHandler> logger, IRaceRepository raceRepository) : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public Task<IReadOnlyCollection<RaceListItemResultModel>> Handle(GetAllRacesQuery request, CancellationToken cancellationToken)
    {
        return _raceRepository.GetAllAsync(cancellationToken);
    }
}
