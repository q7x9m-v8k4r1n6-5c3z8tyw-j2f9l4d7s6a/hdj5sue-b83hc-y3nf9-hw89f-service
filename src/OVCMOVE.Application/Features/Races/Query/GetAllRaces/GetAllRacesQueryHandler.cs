using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetAllRaces;

public class GetAllRacesQueryHandler :
    BaseQueryHandler<GetAllRacesQueryHandler>,
    IRequestHandler<GetAllRacesQuery, PagedResult<RaceItemResultModel>>
{
    private readonly IRaceRepository _raceRepository;

    public GetAllRacesQueryHandler(ILogger<GetAllRacesQueryHandler> logger, IRaceRepository raceRepository) : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public async Task<PagedResult<RaceItemResultModel>> Handle(GetAllRacesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var racesFromDb = await _raceRepository.GetAllAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return new PagedResult<RaceItemResultModel>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = racesFromDb.Count,
            Items = racesFromDb
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray()
        };
    }
}
