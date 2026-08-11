using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races.Command.CreateRace;

/// <summary>
/// Verifies all user relationships before create-race writes or blob uploads
/// begin. This protects integrity because the database has no foreign keys.
/// </summary>
public sealed class CreateRaceRelationValidator
{
    private readonly ITeamRepository _teamRepository;
    private readonly IOrganizerRepository _organizerRepository;

    public CreateRaceRelationValidator(
        ITeamRepository teamRepository,
        IOrganizerRepository organizerRepository)
    {
        _teamRepository = teamRepository;
        _organizerRepository = organizerRepository;
    }

    /// <summary>Rejects missing team and organizer IDs using two batch queries.</summary>
    public async Task ValidateAsync(
        CreateRaceCommand request,
        CancellationToken cancellationToken)
    {
        var teamIds = (request.TeamIds ?? []).Distinct().ToArray();
        var organizerIds = (request.OrganizerIds ?? [])
            .Concat((request.Booths ?? [])
                .SelectMany(booth => booth.OrganizerIds ?? []))
            .Distinct()
            .ToArray();

        var existingTeamIds = (await _teamRepository.GetExistingIdsAsync(
            teamIds,
            cancellationToken)).ToHashSet();
        var existingOrganizerIds =
            (await _organizerRepository.GetExistingIdsAsync(
                organizerIds,
                cancellationToken)).ToHashSet();

        ThrowIfMissing("Team", teamIds, existingTeamIds);
        ThrowIfMissing("Organizer", organizerIds, existingOrganizerIds);
        ThrowIfOrganizerManagesMultipleBooths(request.Booths ?? []);
    }

    private static void ThrowIfOrganizerManagesMultipleBooths(
        IEnumerable<CreateRaceCommand.CreateBoothModel> booths)
    {
        var duplicatedOrganizerIds = booths
            .SelectMany((booth, boothIndex) =>
                (booth.OrganizerIds ?? [])
                    .Distinct()
                    .Select(organizerId => new { organizerId, boothIndex }))
            .GroupBy(item => item.organizerId)
            .Where(group => group.Select(item => item.boothIndex).Distinct().Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicatedOrganizerIds.Length > 0)
        {
            throw new ApplicationValidationException(
                "Mỗi organizer chỉ được quản lý một booth trong một trận đấu: " +
                string.Join(", ", duplicatedOrganizerIds));
        }
    }

    private static void ThrowIfMissing(
        string relationName,
        IEnumerable<Guid> requestedIds,
        IReadOnlySet<Guid> existingIds)
    {
        var missingIds = requestedIds
            .Where(id => id == Guid.Empty || !existingIds.Contains(id))
            .ToArray();
        if (missingIds.Length > 0)
        {
            throw new ApplicationValidationException(
                $"{relationName} không tồn tại: {string.Join(", ", missingIds)}.");
        }
    }
}
