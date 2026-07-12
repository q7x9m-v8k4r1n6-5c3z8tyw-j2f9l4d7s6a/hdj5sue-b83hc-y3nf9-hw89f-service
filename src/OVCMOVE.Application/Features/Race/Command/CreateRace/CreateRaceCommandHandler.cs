using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Command.CreateRace;

public class CreateRaceCommandHandler :
    BaseCommandHandler<CreateRaceCommandHandler>,
    IRequestHandler<CreateRaceCommand, RaceResultModel.AdminRaceDetailResultModel>
{
    private readonly IRaceRepository _raceRepository;

    public CreateRaceCommandHandler(ILogger<CreateRaceCommandHandler> logger, IRaceRepository raceRepository)
        : base(logger)
    {
        _raceRepository = raceRepository;
    }

    public Task<RaceResultModel.AdminRaceDetailResultModel> Handle(CreateRaceCommand request, CancellationToken cancellationToken)
    {
        return _raceRepository.CreateAsync(request, cancellationToken);
    }
}
