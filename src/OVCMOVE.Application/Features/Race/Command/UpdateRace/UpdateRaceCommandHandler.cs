using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Command.UpdateRace;

public class UpdateRaceCommandHandler :
    BaseCommandHandler<UpdateRaceCommandHandler>,
    IRequestHandler<UpdateRaceCommand, RaceResultModel.AdminRaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;

    public UpdateRaceCommandHandler(ILogger<UpdateRaceCommandHandler> logger, IRaceRepository raceRepository)
        : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public Task<RaceResultModel.AdminRaceDetailResultModel?> Handle(UpdateRaceCommand request, CancellationToken cancellationToken)
    {
        return _raceRepository.UpdateAsync(request.RaceId, request, cancellationToken);
    }
}
