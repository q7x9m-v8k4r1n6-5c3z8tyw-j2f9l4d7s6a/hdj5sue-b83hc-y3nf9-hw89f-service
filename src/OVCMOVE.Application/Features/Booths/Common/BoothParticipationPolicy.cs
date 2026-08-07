using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Booths.Common;

public static class BoothParticipationPolicy
{
    public const string ChooseAnotherBoothMessage = "Vui lòng chọn trạm khác.";

    public static string? GetEntryError(
        Booth booth,
        BoothProgressResultModel progress)
    {
        if (!progress.IsTeamInRace)
        {
            return "Đội không tham gia giải đua của trạm này.";
        }

        if (progress.HasCompletedBooth)
        {
            return ChooseAnotherBoothMessage;
        }

        if (!booth.IsHidden)
        {
            return null;
        }

        var hiddenBoothQuota = Math.Min(
            BoothConstants.ParticipationRule.MaximumHiddenBooths,
            progress.CompletedRegularBooths /
                BoothConstants.ParticipationRule.RegularBoothsPerHiddenBooth);

        return progress.CompletedHiddenBooths < hiddenBoothQuota
            ? null
            : ChooseAnotherBoothMessage;
    }
}
