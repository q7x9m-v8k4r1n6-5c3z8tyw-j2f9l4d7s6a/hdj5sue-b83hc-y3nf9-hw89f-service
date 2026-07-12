using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Query.GetAdminRaceList;

public class GetAdminRaceListQueryHandler :
    BaseQueryHandler<GetAdminRaceListQueryHandler>,
    IRequestHandler<GetAdminRaceListQuery, RaceResultModel.AdminRaceListResultModel>
{
    private readonly IRaceRepository _raceRepository;

    public GetAdminRaceListQueryHandler(ILogger<GetAdminRaceListQueryHandler> logger, IRaceRepository raceRepository)
        : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public Task<RaceResultModel.AdminRaceListResultModel> Handle(GetAdminRaceListQuery request, CancellationToken cancellationToken)
    {
        return _raceRepository.GetAdminRaceListAsync(cancellationToken);
    }
}
