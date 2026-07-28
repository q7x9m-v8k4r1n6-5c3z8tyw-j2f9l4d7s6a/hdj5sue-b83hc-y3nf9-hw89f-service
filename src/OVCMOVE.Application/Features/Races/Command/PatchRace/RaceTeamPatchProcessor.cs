using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Races.Command.PatchRace;

public sealed class RaceTeamPatchProcessor
{
    private readonly IRaceTeamRepository _repository;
    private readonly ITeamRepository _teamRepository;

    public RaceTeamPatchProcessor(
        IRaceTeamRepository repository,
        ITeamRepository teamRepository)
    {
        _repository = repository;
        _teamRepository = teamRepository;
    }

    /// <summary>Applies team relationship changes for one race.</summary>
    public async Task ApplyAsync(
        PatchRaceCommand request,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.RaceTeams is null)
        {
            return;
        }

        await ValidateAddedTeamsAsync(
            request.RaceTeams,
            cancellationToken);

        var ids = (await _repository.GetTeamIdsByRaceIdAsync(
            request.RaceId,
            cancellationToken)).ToHashSet();

        foreach (var replacement in request.RaceTeams.Replace ?? [])
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

        foreach (var teamId in request.RaceTeams.Remove?.Distinct() ?? [])
        {
            EnsureExisting(ids, teamId, request.RaceId);
            await _repository.DeleteAsync(
                request.RaceId,
                teamId,
                cancellationToken);
        }

        foreach (var teamId in request.RaceTeams.Add?.Distinct() ?? [])
        {
            await AddIfMissingAsync(
                ids,
                request.RaceId,
                teamId,
                actor,
                now,
                cancellationToken);
        }
    }

    private async Task ValidateAddedTeamsAsync(
        PatchRaceCommand.RaceTeamPatchModel patch,
        CancellationToken cancellationToken)
    {
        var requestedIds = (patch.Add ?? [])
            .Concat((patch.Replace ?? []).Select(item => item.NewId))
            .Distinct()
            .ToArray();
        var existingIds = (await _teamRepository.GetExistingIdsAsync(
            requestedIds,
            cancellationToken)).ToHashSet();
        var missingIds = requestedIds
            .Where(id => id == Guid.Empty || !existingIds.Contains(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new ApplicationValidationException(
                $"Team không tồn tại: {string.Join(", ", missingIds)}.");
        }
    }

    private async Task AddIfMissingAsync(
        HashSet<Guid> ids,
        Guid raceId,
        Guid teamId,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!ids.Add(teamId))
        {
            return;
        }

        await _repository.CreateAsync(new RaceTeam
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            TeamId = teamId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        }, cancellationToken);
    }

    private static void EnsureExisting(
        HashSet<Guid> ids,
        Guid teamId,
        Guid raceId)
    {
        if (!ids.Remove(teamId))
        {
            throw new ApplicationValidationException(
                $"Team '{teamId}' không thuộc trận đấu '{raceId}'.");
        }
    }
}
