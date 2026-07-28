using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races;

namespace OVCMOVE.Application.Features.Races.Command.PatchRace;

internal static class RacePatchMapper
{
    /// <summary>Applies scalar patch values to an existing race entity.</summary>
    public static void Apply(
        Race race,
        PatchRaceCommand request,
        string actor,
        DateTime now)
    {
        ApplyBasicInfo(race, request.BasicInfo);

        if (request.RaceSettings?.IsToggledLeaderboard is bool leaderboard)
        {
            race.IsToggledLeaderboard = leaderboard;
        }

        if (request.RaceSettings?.IsHiddenPoint is bool hiddenPoint)
        {
            race.IsHiddenPoint = hiddenPoint;
        }

        race.ModifiedAt = now;
        race.ModifiedBy = actor;
        RaceInputRules.ValidateRace(
            race.RaceName,
            race.Place,
            race.TimeStart,
            race.TimeEnd);
    }

    private static void ApplyBasicInfo(
        Race race,
        PatchRaceCommand.BasicInfoPatchModel? patch)
    {
        if (patch is null)
        {
            return;
        }

        if (patch.RaceName is not null) race.RaceName = patch.RaceName.Trim();
        if (patch.TimeStart.HasValue) race.TimeStart = patch.TimeStart.Value;
        if (patch.TimeEnd.HasValue) race.TimeEnd = patch.TimeEnd.Value;
        if (patch.Place is not null) race.Place = patch.Place.Trim();
        if (patch.CoverUrl is not null)
        {
            race.CoverUrl = string.IsNullOrWhiteSpace(patch.CoverUrl)
                ? null
                : patch.CoverUrl.Trim();
        }

        if (patch.Status is not null)
        {
            race.Status = NormalizeStatus(patch.Status);
        }
    }

    private static string NormalizeStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            RaceConstants.RaceStatus.Draft => RaceConstants.RaceStatus.Draft,
            RaceConstants.RaceStatus.Ready => RaceConstants.RaceStatus.Ready,
            RaceConstants.RaceStatus.Ongoing => RaceConstants.RaceStatus.Ongoing,
            RaceConstants.RaceStatus.Paused => RaceConstants.RaceStatus.Paused,
            RaceConstants.RaceStatus.Completed =>
                RaceConstants.RaceStatus.Completed,
            _ => throw new ApplicationValidationException(
                $"Trạng thái trận đấu '{status}' không hợp lệ.")
        };
}
