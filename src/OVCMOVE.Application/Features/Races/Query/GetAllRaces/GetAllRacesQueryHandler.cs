using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetAllRaces;

public class GetAllRacesQueryHandler :
    BaseQueryHandler<GetAllRacesQueryHandler>,
    IRequestHandler<GetAllRacesQuery, RaceListItemResultModel>
{
    private readonly IRaceRepository _raceRepository;

    public GetAllRacesQueryHandler(ILogger<GetAllRacesQueryHandler> logger, IRaceRepository raceRepository) : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public async Task<RaceListItemResultModel> Handle(GetAllRacesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var racesFromDb = await _raceRepository.GetAllAsync(cancellationToken);
        return new RaceListItemResultModel
        {
            TotalCount = racesFromDb.Count, // Đếm tổng số lượng phần tử
            Items = racesFromDb            // Đổ mảng dữ liệu vào khay Items
        };
    }
}
