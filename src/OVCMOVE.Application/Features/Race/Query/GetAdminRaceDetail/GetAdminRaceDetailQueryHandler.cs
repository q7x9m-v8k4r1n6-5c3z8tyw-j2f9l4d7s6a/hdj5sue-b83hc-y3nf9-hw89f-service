using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Query.GetAdminRaceDetail;

public class GetAdminRaceDetailQueryHandler :
    BaseQueryHandler<GetAdminRaceDetailQueryHandler>,
    IRequestHandler<GetAdminRaceDetailQuery, RaceResultModel.AdminRaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;

    public GetAdminRaceDetailQueryHandler(ILogger<GetAdminRaceDetailQueryHandler> logger, IRaceRepository raceRepository)
        : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public Task<RaceResultModel.AdminRaceDetailResultModel?> Handle(GetAdminRaceDetailQuery request, CancellationToken cancellationToken)
    {
        return _raceRepository.GetAdminRaceDetailAsync(request.RaceId, cancellationToken);
    }
}
