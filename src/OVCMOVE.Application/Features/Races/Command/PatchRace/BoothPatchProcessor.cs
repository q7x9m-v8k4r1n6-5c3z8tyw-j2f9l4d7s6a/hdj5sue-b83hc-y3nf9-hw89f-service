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
                booth.Description);
        }

        await ValidateOrganizerIdsAsync(
            request.Booths,
            cancellationToken);

        var booths = (await _boothRepository.GetByRaceIdAsync(
            request.RaceId,
            cancellationToken)).ToDictionary(booth => booth.Id);

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
                booth.Description);
            await _boothRepository.UpdateAsync(booth, cancellationToken);
            if (patch.OrganizerIds is not null)
            {
                await ReplaceOrganizerIdsAsync(
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
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        };

    private static ApplicationValidationException InvalidBooth(
        Guid raceId,
        Guid boothId) => new(
        $"Booth '{boothId}' không thuộc trận đấu '{raceId}'.");

    private async Task ReplaceOrganizerIdsAsync(
        Guid boothId,
        IEnumerable<Guid>? organizerIds,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _boothOrganizerRepository.DeleteByBoothIdAsync(
            boothId,
            cancellationToken);
        await CreateOrganizerRelationsAsync(
            boothId,
            organizerIds,
            actor,
            now,
            cancellationToken);
    }

    private async Task CreateOrganizerRelationsAsync(
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
