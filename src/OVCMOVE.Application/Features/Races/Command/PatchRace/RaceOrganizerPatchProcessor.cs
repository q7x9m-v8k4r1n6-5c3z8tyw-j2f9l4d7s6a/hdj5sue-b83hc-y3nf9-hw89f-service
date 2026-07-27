using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Races.Command.PatchRace;

public sealed class RaceOrganizerPatchProcessor
{
    private readonly IRaceOrganizerRepository _repository;
    private readonly IOrganizerRepository _organizerRepository;

    public RaceOrganizerPatchProcessor(
        IRaceOrganizerRepository repository,
        IOrganizerRepository organizerRepository)
    {
        _repository = repository;
        _organizerRepository = organizerRepository;
    }

    /// <summary>Applies organizer relationship changes for one race.</summary>
    public async Task ApplyAsync(
        PatchRaceCommand request,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Organizers is null)
        {
            return;
        }

        await ValidateAddedOrganizersAsync(
            request.Organizers,
            cancellationToken);

        var ids = (await _repository.GetOrganizerIdsByRaceIdAsync(
            request.RaceId,
            cancellationToken)).ToHashSet();

        foreach (var replacement in request.Organizers.Replace ?? [])
        {
            EnsureExisting(ids, replacement.CurrentId, request.RaceId);
            await _repository.DeleteAsync(
                request.RaceId,
                replacement.CurrentId,
                cancellationToken);
            await AddIfMissingAsync(
                ids,
                request.RaceId,
                replacement.NewId,
                actor,
                now,
                cancellationToken);
        }

        foreach (var organizerId in request.Organizers.Remove?.Distinct() ?? [])
        {
            EnsureExisting(ids, organizerId, request.RaceId);
            await _repository.DeleteAsync(
                request.RaceId,
                organizerId,
                cancellationToken);
        }

        foreach (var organizerId in request.Organizers.Add?.Distinct() ?? [])
        {
            await AddIfMissingAsync(
                ids,
                request.RaceId,
                organizerId,
                actor,
                now,
                cancellationToken);
        }
    }

    private async Task ValidateAddedOrganizersAsync(
        PatchRaceCommand.OrganizerPatchModel patch,
        CancellationToken cancellationToken)
    {
        var requestedIds = (patch.Add ?? [])
            .Concat((patch.Replace ?? []).Select(item => item.NewId))
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
                $"Organizer không tồn tại: {string.Join(", ", missingIds)}.");
        }
    }

    private async Task AddIfMissingAsync(
        HashSet<Guid> ids,
        Guid raceId,
        Guid organizerId,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!ids.Add(organizerId))
        {
            return;
        }

        await _repository.CreateAsync(new RaceOrganizer
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            OrganizerId = organizerId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        }, cancellationToken);
    }

    private static void EnsureExisting(
        HashSet<Guid> ids,
        Guid organizerId,
        Guid raceId)
    {
        if (!ids.Remove(organizerId))
        {
            throw new ApplicationValidationException(
                $"Organizer '{organizerId}' không thuộc trận đấu '{raceId}'.");
        }
    }
}
