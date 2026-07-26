using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Races.Command.PatchRace;

public class PatchRaceCommandHandler :
    BaseCommandHandler<PatchRaceCommandHandler>,
    IRequestHandler<PatchRaceCommand, RaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothRepository _boothRepository;
    private readonly IRaceTeamRepository _raceTeamRepository;
    private readonly IRaceOrganizerRepository _raceOrganizerRepository;

    public PatchRaceCommandHandler(
        ILogger<PatchRaceCommandHandler> logger,
        IMapper mapper,
        IRaceRepository raceRepository,
        IBoothRepository boothRepository,
        IRaceTeamRepository raceTeamRepository,
        IRaceOrganizerRepository raceOrganizerRepository,
        IUnitOfWork unitOfWork) : base(logger, mapper, unitOfWork)
    {
        _raceRepository = raceRepository;
        _boothRepository = boothRepository;
        _raceTeamRepository = raceTeamRepository;
        _raceOrganizerRepository = raceOrganizerRepository;
    }

    public async Task<RaceDetailResultModel?> Handle(PatchRaceCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var unitOfWork = _unitOfWork
            ?? throw new InvalidOperationException("Unit of work is not configured.");

        unitOfWork.Begin();

        try
        {
            var existingRace = await _raceRepository.GetByIdAsync(request.RaceId, cancellationToken);
            if (existingRace is null)
            {
                unitOfWork.Rollback();
                return null;
            }

            if (string.Equals(existingRace.Status, RaceConstants.RaceStatus.Completed, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Khong the cap nhat Race da ket thuc.");
            }

            var actor = ResolveActor(request);
            var now = DateTime.UtcNow;

            ApplyRacePatch(existingRace, request, actor, now);

            var updated = await _raceRepository.UpdateAsync(existingRace, cancellationToken);
            if (!updated)
            {
                unitOfWork.Rollback();
                return null;
            }

            await ApplyBoothPatchAsync(request, actor, now, cancellationToken);
            await ApplyRaceTeamPatchAsync(request, actor, now, cancellationToken);
            await ApplyOrganizerPatchAsync(request, actor, now, cancellationToken);

            unitOfWork.Commit();
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }

        return await _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
    }

    private async Task ApplyBoothPatchAsync(
        PatchRaceCommand request,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Booths is null)
        {
            return;
        }

        var existingBooths = (await _boothRepository.GetByRaceIdAsync(request.RaceId, cancellationToken))
            .ToDictionary(booth => booth.Id);

        foreach (var boothId in request.Booths.Remove?.Distinct() ?? Enumerable.Empty<Guid>())
        {
            if (!existingBooths.Remove(boothId))
            {
                throw new InvalidOperationException($"Booth '{boothId}' does not belong to race '{request.RaceId}'.");
            }

            await _boothRepository.DeleteAsync(boothId, cancellationToken);
        }

        foreach (var boothPatch in request.Booths.Update ?? Enumerable.Empty<PatchRaceCommand.UpdateBoothPatchItem>())
        {
            if (!existingBooths.TryGetValue(boothPatch.BoothId, out var existingBooth))
            {
                throw new InvalidOperationException($"Booth '{boothPatch.BoothId}' does not belong to race '{request.RaceId}'.");
            }

            if (boothPatch.Name is not null)
            {
                existingBooth.Name = boothPatch.Name.Trim();
            }

            if (boothPatch.Place is not null)
            {
                existingBooth.Place = boothPatch.Place.Trim();
            }

            if (boothPatch.Description is not null)
            {
                existingBooth.Description = boothPatch.Description.Trim();
            }

            if (boothPatch.OrganizerIds is not null)
            {
                existingBooth.BoothOrganizerID = SerializeOrganizerIds(boothPatch.OrganizerIds);
            }

            existingBooth.ModifiedAt = now;
            existingBooth.ModifiedBy = actor;

            await _boothRepository.UpdateAsync(existingBooth, cancellationToken);
        }

        foreach (var boothPatch in request.Booths.Add ?? Enumerable.Empty<PatchRaceCommand.CreateBoothPatchItem>())
        {
            var booth = new Booth
            {
                Id = Guid.NewGuid(),
                RaceID = request.RaceId,
                Name = boothPatch.Name.Trim(),
                Place = boothPatch.Place.Trim(),
                Description = boothPatch.Description?.Trim() ?? string.Empty,
                BoothOrganizerID = SerializeOrganizerIds(boothPatch.OrganizerIds),
                CreatedAt = now,
                CreatedBy = actor,
                ModifiedAt = now,
                ModifiedBy = actor,
                IsDeleted = false
            };

            await _boothRepository.CreateAsync(booth, cancellationToken);
        }
    }

    private async Task ApplyRaceTeamPatchAsync(
        PatchRaceCommand request,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.RaceTeams is null)
        {
            return;
        }

        var teamIds = (await _raceTeamRepository.GetTeamIdsByRaceIdAsync(request.RaceId, cancellationToken)).ToHashSet();

        foreach (var replacement in request.RaceTeams.Replace ?? Enumerable.Empty<PatchRaceCommand.ReplaceRelationItem>())
        {
            if (!teamIds.Remove(replacement.CurrentId))
            {
                throw new InvalidOperationException($"Team '{replacement.CurrentId}' does not belong to race '{request.RaceId}'.");
            }

            await _raceTeamRepository.DeleteAsync(request.RaceId, replacement.CurrentId, cancellationToken);

            if (teamIds.Add(replacement.NewId))
            {
                await _raceTeamRepository.CreateAsync(CreateRaceTeam(request.RaceId, replacement.NewId, actor, now), cancellationToken);
            }
        }

        foreach (var teamId in request.RaceTeams.Remove?.Distinct() ?? Enumerable.Empty<Guid>())
        {
            if (!teamIds.Remove(teamId))
            {
                throw new InvalidOperationException($"Team '{teamId}' does not belong to race '{request.RaceId}'.");
            }

            await _raceTeamRepository.DeleteAsync(request.RaceId, teamId, cancellationToken);
        }

        foreach (var teamId in request.RaceTeams.Add?.Distinct() ?? Enumerable.Empty<Guid>())
        {
            if (!teamIds.Add(teamId))
            {
                continue;
            }

            await _raceTeamRepository.CreateAsync(CreateRaceTeam(request.RaceId, teamId, actor, now), cancellationToken);
        }
    }

    private async Task ApplyOrganizerPatchAsync(
        PatchRaceCommand request,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Organizers is null)
        {
            return;
        }

        var organizerIds = (await _raceOrganizerRepository.GetOrganizerIdsByRaceIdAsync(request.RaceId, cancellationToken)).ToHashSet();

        foreach (var replacement in request.Organizers.Replace ?? Enumerable.Empty<PatchRaceCommand.ReplaceRelationItem>())
        {
            if (!organizerIds.Remove(replacement.CurrentId))
            {
                throw new InvalidOperationException($"Organizer '{replacement.CurrentId}' does not belong to race '{request.RaceId}'.");
            }

            await _raceOrganizerRepository.DeleteAsync(request.RaceId, replacement.CurrentId, cancellationToken);

            if (organizerIds.Add(replacement.NewId))
            {
                await _raceOrganizerRepository.CreateAsync(CreateRaceOrganizer(request.RaceId, replacement.NewId, actor, now), cancellationToken);
            }
        }

        foreach (var organizerId in request.Organizers.Remove?.Distinct() ?? Enumerable.Empty<Guid>())
        {
            if (!organizerIds.Remove(organizerId))
            {
                throw new InvalidOperationException($"Organizer '{organizerId}' does not belong to race '{request.RaceId}'.");
            }

            await _raceOrganizerRepository.DeleteAsync(request.RaceId, organizerId, cancellationToken);
        }

        foreach (var organizerId in request.Organizers.Add?.Distinct() ?? Enumerable.Empty<Guid>())
        {
            if (!organizerIds.Add(organizerId))
            {
                continue;
            }

            await _raceOrganizerRepository.CreateAsync(CreateRaceOrganizer(request.RaceId, organizerId, actor, now), cancellationToken);
        }
    }

    private static void ApplyRacePatch(Race race, PatchRaceCommand request, string actor, DateTime now)
    {
        if (request.BasicInfo is not null)
        {
            if (request.BasicInfo.RaceName is not null)
            {
                race.RaceName = request.BasicInfo.RaceName.Trim();
            }

            if (request.BasicInfo.TimeStart.HasValue)
            {
                race.TimeStart = request.BasicInfo.TimeStart.Value;
            }

            if (request.BasicInfo.TimeEnd.HasValue)
            {
                race.TimeEnd = request.BasicInfo.TimeEnd.Value;
            }

            if (request.BasicInfo.Place is not null)
            {
                race.Place = request.BasicInfo.Place.Trim();
            }

            if (request.BasicInfo.CoverUrl is not null)
            {
                race.CoverUrl = string.IsNullOrWhiteSpace(request.BasicInfo.CoverUrl)
                    ? null
                    : request.BasicInfo.CoverUrl.Trim();
            }

            if (request.BasicInfo.Status is not null)
            {
                race.Status = NormalizeRaceStatus(request.BasicInfo.Status);
            }
        }

        if (request.RaceSettings is not null)
        {
            if (request.RaceSettings.IsToggledLeaderboard.HasValue)
            {
                race.IsToggledLeaderboard = request.RaceSettings.IsToggledLeaderboard.Value;
            }

            if (request.RaceSettings.IsHiddenPoint.HasValue)
            {
                race.IsHiddenPoint = request.RaceSettings.IsHiddenPoint.Value;
            }
        }

        race.ModifiedAt = now;
        race.ModifiedBy = actor;
    }

    private static RaceTeam CreateRaceTeam(Guid raceId, Guid teamId, string actor, DateTime now)
    {
        return new RaceTeam
        {
            Id = Guid.NewGuid(),
            RaceID = raceId,
            TeamID = teamId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor,
            IsDeleted = false
        };
    }

    private static RaceOrganizer CreateRaceOrganizer(Guid raceId, Guid organizerId, string actor, DateTime now)
    {
        return new RaceOrganizer
        {
            Id = Guid.NewGuid(),
            RaceID = raceId,
            OrganizerID = organizerId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor,
            IsDeleted = false
        };
    }

    private static string ResolveActor(PatchRaceCommand request)
    {
        return string.IsNullOrWhiteSpace(request.ModifiedBy)
            ? "system"
            : request.ModifiedBy.Trim();
    }

    private static string SerializeOrganizerIds(IEnumerable<Guid> organizerIds)
    {
        return string.Join(',', organizerIds.Distinct());
    }

    private static string NormalizeRaceStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            RaceConstants.RaceStatus.Ready => RaceConstants.RaceStatus.Ready,
            RaceConstants.RaceStatus.Ongoing => RaceConstants.RaceStatus.Ongoing,
            RaceConstants.RaceStatus.Paused => RaceConstants.RaceStatus.Paused,
            RaceConstants.RaceStatus.Completed => RaceConstants.RaceStatus.Completed,
            _ => RaceConstants.RaceStatus.Draft,
        };
    }
}
