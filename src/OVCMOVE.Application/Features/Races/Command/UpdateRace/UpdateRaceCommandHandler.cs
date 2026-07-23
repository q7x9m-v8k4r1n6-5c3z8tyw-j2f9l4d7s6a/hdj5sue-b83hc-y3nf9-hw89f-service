using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Races.Command;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Races.Command.UpdateRace;

public class UpdateRaceCommandHandler :
    BaseCommandHandler<UpdateRaceCommandHandler>,
    IRequestHandler<UpdateRaceCommand, RaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothRepository _boothRepository;
    private readonly IRaceTeamRepository _raceTeamRepository;
    private readonly IRaceOrganizerRepository _raceOrganizerRepository;

    public UpdateRaceCommandHandler(
        ILogger<UpdateRaceCommandHandler> logger,
        IMapper mapper,
        IRaceRepository raceRepository,
        IBoothRepository boothRepository,
        IRaceTeamRepository raceTeamRepository,
        IRaceOrganizerRepository raceOrganizerRepository) : base(logger, mapper)
    {
        _raceRepository = raceRepository;
        _boothRepository = boothRepository;
        _raceTeamRepository = raceTeamRepository;
        _raceOrganizerRepository = raceOrganizerRepository;
    }

    public async Task<RaceDetailResultModel?> Handle(UpdateRaceCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingRace = await _raceRepository.GetByIdAsync(request.RaceId, cancellationToken);
        if (existingRace is null) return null;

        if (string.Equals(existingRace.Status, RaceConstants.RaceStatus.Completed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Khong the cap nhat Race da ket thuc.");
        }

        var updatedRace = RaceCommandMapper.BuildRace(
            request.RaceId,
            request.RaceName,
            request.TimeStart,
            request.TimeEnd,
            request.Place,
            request.CoverUrl,
            request.IsToggledLeaderboard,
            request.IsHiddenPoint,
            existingRace.CreatedAt,
            existingRace.CreatedBy,
            request.Status ?? existingRace.Status);

        var updated = await _raceRepository.UpdateAsync(updatedRace, cancellationToken);
        if (!updated) return null;

        await _boothRepository.DeleteByRaceIdAsync(request.RaceId, cancellationToken);
        await _raceTeamRepository.DeleteByRaceIdAsync(request.RaceId, cancellationToken);
        await _raceOrganizerRepository.DeleteByRaceIdAsync(request.RaceId, cancellationToken);

        foreach (var boothInput in request.Booth)
        {
            await _boothRepository.CreateAsync(RaceCommandMapper.BuildBooth(request.RaceId, boothInput), cancellationToken);
        }

        foreach (var teamInput in request.RaceTeam)
        {
            await _raceTeamRepository.CreateAsync(RaceCommandMapper.BuildRaceTeam(request.RaceId, teamInput), cancellationToken);
        }

        foreach (var organizerId in request.OrganizerId.Where(id => id.HasValue).Select(id => id!.Value))
        {
            await _raceOrganizerRepository.CreateAsync(RaceCommandMapper.BuildRaceOrganizer(request.RaceId, organizerId), cancellationToken);
        }

        return await _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
    }
}
