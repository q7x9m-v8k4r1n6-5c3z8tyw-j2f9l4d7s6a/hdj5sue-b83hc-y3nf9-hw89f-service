using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Entities;

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

        var raceId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var race = _mapper.Map<Race>(request);
        SetAuditFields(race, raceId, now, "system", now, "system");

        var createdRaceId = await _raceRepository.CreateAsync(race, cancellationToken);
        if (createdRaceId is null) return null;

        foreach (var boothInput in request.Booth)
        {
            var booth = _mapper.Map<Booth>(boothInput);
            SetAuditFields(booth, Guid.NewGuid(), now, "system", now, "system");
            booth.RaceID = raceId;

            await _boothRepository.CreateAsync(booth, cancellationToken);
        }

        foreach (var teamInput in request.RaceTeam)
        {
            var raceTeam = _mapper.Map<RaceTeam>(teamInput);
            SetAuditFields(raceTeam, Guid.NewGuid(), now, "system", now, "system");
            raceTeam.RaceID = raceId;

            await _raceTeamRepository.CreateAsync(raceTeam, cancellationToken);
        }

        foreach (var organizerId in request.OrganizerId.Where(id => id.HasValue).Select(id => id!.Value))
        {
            var raceOrganizer = _mapper.Map<RaceOrganizer>(organizerId);
            SetAuditFields(raceOrganizer, Guid.NewGuid(), now, "system", now, "system");
            raceOrganizer.RaceID = raceId;

            await _raceOrganizerRepository.CreateAsync(raceOrganizer, cancellationToken);
        }

        return raceId;
    }

    private static void SetAuditFields(
        BaseEntity entity,
        Guid id,
        DateTime createdAt,
        string? createdBy,
        DateTime modifiedAt,
        string? modifiedBy)
    {
        entity.Id = id;
        entity.CreatedAt = createdAt;
        entity.CreatedBy = createdBy;
        entity.ModifiedAt = modifiedAt;
        entity.ModifiedBy = modifiedBy;
        entity.IsDeleted = false;
    }
}
