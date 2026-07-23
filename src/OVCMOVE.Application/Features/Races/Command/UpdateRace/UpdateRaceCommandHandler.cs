using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Common;
using OVCMOVE.Domain.Entities;

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

        request.Status ??= existingRace.Status;

        var now = DateTime.UtcNow;
        var updatedRace = _mapper.Map<Race>(request);
        SetAuditFields(updatedRace, request.RaceId, existingRace.CreatedAt, existingRace.CreatedBy, now, "system");

        var updated = await _raceRepository.UpdateAsync(updatedRace, cancellationToken);
        if (!updated) return null;

        await _boothRepository.DeleteByRaceIdAsync(request.RaceId, cancellationToken);
        await _raceTeamRepository.DeleteByRaceIdAsync(request.RaceId, cancellationToken);
        await _raceOrganizerRepository.DeleteByRaceIdAsync(request.RaceId, cancellationToken);

        foreach (var boothInput in request.Booth)
        {
            var booth = _mapper.Map<Booth>(boothInput);
            SetAuditFields(booth, Guid.NewGuid(), now, "system", now, "system");
            booth.RaceID = request.RaceId;

            await _boothRepository.CreateAsync(booth, cancellationToken);
        }

        foreach (var teamInput in request.RaceTeam)
        {
            var raceTeam = _mapper.Map<RaceTeam>(teamInput);
            SetAuditFields(raceTeam, Guid.NewGuid(), now, "system", now, "system");
            raceTeam.RaceID = request.RaceId;

            await _raceTeamRepository.CreateAsync(raceTeam, cancellationToken);
        }

        foreach (var organizerId in request.OrganizerId.Where(id => id.HasValue).Select(id => id!.Value))
        {
            var raceOrganizer = _mapper.Map<RaceOrganizer>(organizerId);
            SetAuditFields(raceOrganizer, Guid.NewGuid(), now, "system", now, "system");
            raceOrganizer.RaceID = request.RaceId;

            await _raceOrganizerRepository.CreateAsync(raceOrganizer, cancellationToken);
        }

        return await _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
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
