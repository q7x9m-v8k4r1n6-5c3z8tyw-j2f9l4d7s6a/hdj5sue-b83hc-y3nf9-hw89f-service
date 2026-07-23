using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Races.Command;

internal static class RaceCommandMapper
{
    private static string NormalizeRaceStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return RaceConstants.RaceStatus.Draft;

        return status.Trim().ToLowerInvariant() switch
        {
            RaceConstants.RaceStatus.Ready => RaceConstants.RaceStatus.Ready,
            RaceConstants.RaceStatus.Ongoing => RaceConstants.RaceStatus.Ongoing,
            RaceConstants.RaceStatus.Paused => RaceConstants.RaceStatus.Paused,
            RaceConstants.RaceStatus.Completed => RaceConstants.RaceStatus.Completed,
            _ => RaceConstants.RaceStatus.Draft,
        };
    }

    /// <summary>
    /// Application helper: tao entity Race tu du lieu command, khong phu thuoc API hay SQL.
    /// </summary>
    public static Race BuildRace(
        Guid raceId,
        string raceName,
        DateTime timeStart,
        DateTime timeEnd,
        string place,
        string? coverUrl,
        bool isToggledLeaderboard,
        bool isHiddenPoint,
        DateTime createdAt,
        string createdBy,
        string? status = null)
    {
        var now = DateTime.UtcNow;

        return new Race
        {
            Id = raceId,
            RaceName = raceName.Trim(),
            TimeStart = timeStart,
            TimeEnd = timeEnd,
            Place = place.Trim(),
            CoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl.Trim(),
            IsToggledLeaderboard = isToggledLeaderboard,
            IsHiddenPoint = isHiddenPoint,
            Status = NormalizeRaceStatus(status),
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            ModifiedAt = now,
            ModifiedBy = "system",
            IsDeleted = false
        };
    }

    public static Booth BuildBooth(Guid raceId, RaceDto.BoothInput input)
    {
        return new Booth
        {
            Id = Guid.NewGuid(),
            RaceID = raceId,
            Name = input.Name.Trim(),
            Place = input.Place.Trim(),
            Description = input.Description.Trim(),
            BoothOrganizerID = input.OrganizerID.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            ModifiedAt = DateTime.UtcNow,
            ModifiedBy = "system"
        };
    }

    public static RaceTeam BuildRaceTeam(Guid raceId, RaceDto.RaceTeamInputDto input)
    {
        return new RaceTeam
        {
            Id = Guid.NewGuid(),
            RaceID = raceId,
            TeamID = input.TeamID,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            ModifiedAt = DateTime.UtcNow,
            ModifiedBy = "system"
        };
    }

    public static RaceOrganizer BuildRaceOrganizer(Guid raceId, Guid organizerId)
    {
        return new RaceOrganizer
        {
            Id = Guid.NewGuid(),
            RaceID = raceId,
            OrganizerID = organizerId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            ModifiedAt = DateTime.UtcNow,
            ModifiedBy = "system"
        };
    }
}
