using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command;

namespace OVCMOVE.Application.Features.Races.Command.CreateRace;

public class CreateRaceCommandHandler :
    BaseCommandHandler<CreateRaceCommandHandler>,
    IRequestHandler<CreateRaceCommand, Guid?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothRepository _boothRepository;
    private readonly IRaceTeamRepository _raceTeamRepository;
    private readonly IRaceOrganizerRepository _raceOrganizerRepository;

    public CreateRaceCommandHandler(
        ILogger<CreateRaceCommandHandler> logger,
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

    public async Task<Guid?> Handle(CreateRaceCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Cấp phát xung ID chủ động - Siêu an toàn
        var raceId = Guid.NewGuid();
        var race = RaceCommandMapper.BuildRace(
            raceId,
            request.RaceName,
            request.TimeStart,
            request.TimeEnd,
            request.Place,
            request.CoverUrl,
            request.IsToggledLeaderboard,
            request.IsHiddenPoint,
            DateTime.UtcNow,
            "system",
            request.Status);

        var createdRaceId = await _raceRepository.CreateAsync(race, cancellationToken);
        if (createdRaceId is null) return null;

        // 2. Tạo từng Booth (Trạm) theo logic của em
        foreach (var boothInput in request.Booth)
        {
            await _boothRepository.CreateAsync(RaceCommandMapper.BuildBooth(raceId, boothInput), cancellationToken);
        }

        // 3. Tạo từng Team (Đội đua) theo logic của em
        foreach (var teamInput in request.RaceTeam)
        {
            await _raceTeamRepository.CreateAsync(RaceCommandMapper.BuildRaceTeam(raceId, teamInput), cancellationToken);
        }

        // 4. Tạo từng Organizer (Ban tổ chức) theo logic của em
        foreach (var organizerId in request.OrganizerId.Where(id => id.HasValue).Select(id => id!.Value))
        {
            await _raceOrganizerRepository.CreateAsync(RaceCommandMapper.BuildRaceOrganizer(raceId, organizerId), cancellationToken);
        }

        return raceId;
    }
}
