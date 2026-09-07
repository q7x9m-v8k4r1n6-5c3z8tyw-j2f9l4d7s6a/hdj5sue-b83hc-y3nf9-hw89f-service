using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Races.Command.PatchRace;

public sealed class BoothPatchProcessor
{
    private readonly IBoothRepository _boothRepository;
    private readonly IBoothOrganizerRepository _boothOrganizerRepository;
    private readonly IOrganizerRepository _organizerRepository;

    public BoothPatchProcessor(
        IBoothRepository boothRepository,
        IBoothOrganizerRepository boothOrganizerRepository,
        IOrganizerRepository organizerRepository)
    {
        _boothRepository = boothRepository;
        _boothOrganizerRepository = boothOrganizerRepository;
        _organizerRepository = organizerRepository;
    }

    /// <summary>Applies booth additions, updates and removals for one race.</summary>
    public async Task ApplyAsync(
        PatchRaceCommand request,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Booths is null)
        {
            return;
        }

        foreach (var booth in request.Booths.Add ?? [])
        {
            RaceInputRules.ValidateBooth(
                booth.Name,
                booth.Place,
                booth.Description,
                booth.Type,
                booth.MaximumScore);
        }

        await ValidateOrganizerIdsAsync(
            request.Booths,
            cancellationToken);

        var booths = (await _boothRepository.GetByRaceIdAsync(
            request.RaceId,
            cancellationToken)).ToDictionary(booth => booth.Id);

        await ValidateOneBoothPerOrganizerAsync(
            request.RaceId,
            request.Booths,
            booths,
            cancellationToken);

        foreach (var boothId in request.Booths.Remove?.Distinct() ?? [])
        {
            if (!booths.Remove(boothId))
            {
                throw InvalidBooth(request.RaceId, boothId);
            }

            await _boothOrganizerRepository.DeleteByBoothIdAsync(
                boothId,
                cancellationToken);
            await _boothRepository.DeleteAsync(boothId, cancellationToken);
        }

        foreach (var patch in (request.Booths.Update ?? [])
                     .Where(item => item.OrganizerIds is not null))
        {
            if (!booths.ContainsKey(patch.BoothId))
            {
                throw InvalidBooth(request.RaceId, patch.BoothId);
            }

            await _boothOrganizerRepository.DeleteByBoothIdAsync(
                patch.BoothId,
                cancellationToken);
        }

        foreach (var patch in request.Booths.Update ?? [])
        {
            if (!booths.TryGetValue(patch.BoothId, out var booth))
            {
                throw InvalidBooth(request.RaceId, patch.BoothId);
            }

            ApplyUpdate(booth, patch, actor, now);
            RaceInputRules.ValidateBooth(
                booth.Name,
                booth.Place,
                booth.Description,
                booth.Type,
                booth.MaximumScore);
            await _boothRepository.UpdateAsync(booth, cancellationToken);
            if (patch.OrganizerIds is not null)
            {
                await CreateOrganizerRelationsAsync(
                    request.RaceId,
                    booth.Id,
                    patch.OrganizerIds,
                    actor,
                    now,
                    cancellationToken);
            }
        }

        foreach (var patch in request.Booths.Add ?? [])
        {
            var booth = CreateBooth(
                request.RaceId,
                patch,
                actor,
                now);
            await _boothRepository.CreateAsync(
                booth,
                cancellationToken);
            await CreateOrganizerRelationsAsync(
                request.RaceId,
                booth.Id,
                patch.OrganizerIds,
                actor,
                now,
                cancellationToken);
        }
    }

    private async Task ValidateOrganizerIdsAsync(
        PatchRaceCommand.BoothPatchModel patch,
        CancellationToken cancellationToken)
    {
        var requestedIds = (patch.Add ?? [])
            .SelectMany(item => item.OrganizerIds ?? [])
            .Concat((patch.Update ?? [])
                .Where(item => item.OrganizerIds is not null)
                .SelectMany(item => item.OrganizerIds!))
            .Distinct()
            .ToArray();
        var existingIds =
            (await _organizerRepository.GetExistingIdsAsync(
                requestedIds,
                cancellationToken)).ToHashSet();
        var missingIds = requestedIds
            .Where(id => id == Guid.Empty || !existingIds.Contains(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new ApplicationValidationException(
                $"Booth organizer không tồn tại: {string.Join(", ", missingIds)}.");
        }
    }

    private static void ApplyUpdate(
        Booth booth,
        PatchRaceCommand.UpdateBoothPatchItem patch,
        string actor,
        DateTime now)
    {
        if (patch.Name is not null) booth.Name = patch.Name.Trim();
        if (patch.Place is not null) booth.Place = patch.Place.Trim();
        if (patch.Description is not null)
        {
            booth.Description = patch.Description.Trim();
        }
        if (patch.IsHidden is bool isHidden) booth.IsHidden = isHidden;
        if (patch.Type is not null)
        {
            booth.Type = patch.Type.Trim().ToLowerInvariant();
        }
        if (patch.MaximumScore.HasValue) booth.MaximumScore = patch.MaximumScore;

        booth.ModifiedAt = now;
        booth.ModifiedBy = actor;
    }

    private static Booth CreateBooth(
        Guid raceId,
        PatchRaceCommand.CreateBoothPatchItem patch,
        string actor,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            Name = patch.Name.Trim(),
            Place = patch.Place.Trim(),
            Description = patch.Description?.Trim() ?? string.Empty,
            IsHidden = patch.IsHidden,
            Type = patch.Type.Trim().ToLowerInvariant(),
            MaximumScore = patch.MaximumScore,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        };

    private static ApplicationValidationException InvalidBooth(
        Guid raceId,
        Guid boothId) => new(
        $"Booth '{boothId}' không thuộc trận đấu '{raceId}'.");

    private async Task ValidateOneBoothPerOrganizerAsync(
        Guid raceId,
        PatchRaceCommand.BoothPatchModel patch,
        IReadOnlyDictionary<Guid, Booth> booths,
        CancellationToken cancellationToken)
    {
        var assignments = (await _boothOrganizerRepository.GetByRaceIdAsync(
                raceId,
                cancellationToken))
            .GroupBy(item => item.BoothId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.OrganizerId).ToHashSet());

        foreach (var boothId in patch.Remove?.Distinct() ?? [])
        {
            assignments.Remove(boothId);
        }

        foreach (var update in patch.Update ?? [])
        {
            if (update.OrganizerIds is not null && booths.ContainsKey(update.BoothId))
            {
                assignments[update.BoothId] = update.OrganizerIds.ToHashSet();
            }
        }

        var organizerAssignments = assignments.Values
            .Concat((patch.Add ?? [])
                .Select(item => (item.OrganizerIds ?? []).ToHashSet()))
            .SelectMany(ids => ids)
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (organizerAssignments.Length > 0)
        {
            throw new ApplicationValidationException(
                "Mỗi organizer chỉ được quản lý một booth trong một trận đấu: " +
                string.Join(", ", organizerAssignments));
        }
    }

    private async Task CreateOrganizerRelationsAsync(
        Guid raceId,
        Guid boothId,
        IEnumerable<Guid>? organizerIds,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        foreach (var organizerId in (organizerIds ?? []).Distinct())
        {
            await _boothOrganizerRepository.CreateAsync(
                new BoothOrganizer
                {
                    Id = Guid.NewGuid(),
                    RaceId = raceId,
                    BoothId = boothId,
                    OrganizerId = organizerId,
                    CreatedBy = actor,
                    CreatedAt = now,
                    ModifiedBy = actor,
                    ModifiedAt = now
                },
                cancellationToken);
        }
    }
}
