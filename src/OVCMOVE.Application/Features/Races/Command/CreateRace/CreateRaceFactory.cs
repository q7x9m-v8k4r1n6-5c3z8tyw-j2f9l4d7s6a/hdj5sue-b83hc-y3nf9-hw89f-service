using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Features.Races;

namespace OVCMOVE.Application.Features.Races.Command.CreateRace;

/// <summary>
/// Validates create-race input and converts it to the data-only domain entities
/// persisted by the handler.
/// </summary>
internal static class CreateRaceFactory
{
    /// <summary>Rejects invalid race input before any external side effect occurs.</summary>
    internal static void Validate(CreateRaceCommand request)
    {
        RaceInputRules.ValidateRace(
            request.RaceName,
            request.Place,
            request.TimeStart,
            request.TimeEnd);

        foreach (var booth in request.Booths ?? [])
        {
            RaceInputRules.ValidateBooth(
                booth.Name,
                booth.Place,
                booth.Description);
        }
    }

    /// <summary>Creates the race entity with application-owned defaults.</summary>
    internal static Race CreateRace(
        CreateRaceCommand request,
        Guid raceId,
        string? coverUrl,
        string actor,
        DateTime now) => new()
        {
            Id = raceId,
            RaceName = request.RaceName.Trim(),
            TimeStart = request.TimeStart,
            TimeEnd = request.TimeEnd,
            Place = request.Place.Trim(),
            Status = RaceConstants.RaceStatus.Draft,
            CoverUrl = coverUrl,
            Rules = request.Rules?.Trim() ?? string.Empty,
            IsToggledLeaderboard = request.IsToggledLeaderboard,
            IsHiddenPoint = request.IsHiddenPoint,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        };

    /// <summary>Creates one booth belonging to the new race.</summary>
    internal static Booth CreateBooth(
        CreateRaceCommand.CreateBoothModel input,
        Guid raceId,
        string actor,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            Name = input.Name.Trim(),
            Place = input.Place.Trim(),
            Description = input.Description?.Trim() ?? string.Empty,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        };

    /// <summary>Creates one organizer relationship belonging to a booth.</summary>
    internal static BoothOrganizer CreateBoothOrganizer(
        Guid raceId,
        Guid boothId,
        Guid organizerId,
        string actor,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            BoothId = boothId,
            OrganizerId = organizerId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        };

    /// <summary>Creates one team relationship belonging to the new race.</summary>
    internal static RaceTeam CreateRaceTeam(
        Guid raceId,
        Guid teamId,
        string actor,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            TeamId = teamId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        };

    /// <summary>Creates one organizer relationship belonging to the new race.</summary>
    internal static RaceOrganizer CreateRaceOrganizer(
        Guid raceId,
        Guid organizerId,
        string actor,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            RaceId = raceId,
            OrganizerId = organizerId,
            CreatedAt = now,
            CreatedBy = actor,
            ModifiedAt = now,
            ModifiedBy = actor
        };
}
