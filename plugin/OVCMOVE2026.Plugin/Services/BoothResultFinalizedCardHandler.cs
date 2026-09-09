using MediatR;
using MongoDB.Bson;
using OVCMOVE.Application.Abstractions.Plugins;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Domain.Constants;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

/// <summary>
/// Resolves booth bonus cards before Cupid so Cupid always observes the final
/// points awarded for the booth, including Engineer or Athlete.
/// </summary>
public sealed class BoothResultFinalizedCardHandler(
    IRaceCardRepository repository,
    ISender sender) : IPluginEventHandler
{
    public string EventName => PluginEventNames.BoothResultFinalized;

    public async Task<IPluginEventExecution?> PrepareAsync(
        PluginEventContext context,
        CancellationToken cancellationToken)
    {
        var boothResult = context.BoothResult
            ?? throw new ApplicationValidationException(
                "Sự kiện booth finalized thiếu dữ liệu kết quả.");
        if (!context.BoothId.HasValue)
            throw new ApplicationValidationException(
                "Sự kiện booth finalized thiếu boothId.");

        if (await repository.HasPendingReviveAsync(
                context.RaceId,
                context.TeamId,
                context.BoothId.Value,
                cancellationToken))
            throw new ApplicationConflictException(
                "Đội đang chờ quản trạm xác nhận Revive. Hãy xác nhận trước khi kết thúc booth.");

        var effects = await repository.GetActiveBoothResultEffectsAsync(
            context.RaceId,
            context.TeamId,
            context.OccurredAt,
            cancellationToken);
        if (effects.Count == 0) return null;

        var resolutions = new List<CardEffectResolution>();
        var finalAwardedPoints = boothResult.SubmittedPoints;
        var bonusEffect = effects
            .Where(IsBoothBonus)
            .Where(effect => string.Equals(
                effect.Data.GetString("requiredBoothType"),
                boothResult.BoothType,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(effect => effect.StartAt)
            .ThenBy(effect => effect.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        var cupidEffects = effects
            .Where(effect => effect.CardId == CardIds.Cupid)
            .ToArray();
        var firewallEffect = effects
            .Where(effect => effect.CardId == CardIds.Firewall)
            .Where(effect => effect.TargetBoothId == context.BoothId.Value.ToString())
            .OrderBy(effect => effect.StartAt)
            .ThenBy(effect => effect.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        var claimedEffectIds = cupidEffects.Select(effect => effect.Id).ToList();
        if (bonusEffect is not null)
            claimedEffectIds.Add(bonusEffect.Id);
        if (firewallEffect is not null)
            claimedEffectIds.Add(firewallEffect.Id);
        if (claimedEffectIds.Count == 0) return null;

        try
        {
            await repository.ClaimEffectsAsync(
                context.RaceId,
                claimedEffectIds,
                context.EventId,
                context.OccurredAt,
                cancellationToken);

            if (bonusEffect is not null)
            {
                var qualified = IsQualified(bonusEffect, boothResult);
                var bonusPoints = qualified
                    ? CalculateBonus(
                        boothResult.SubmittedPoints,
                        bonusEffect.Data.GetDouble("scoreMultiplier", 2))
                    : 0;

                if (bonusPoints != 0)
                {
                    await AdjustScoreAsync(
                        context,
                        context.TeamId,
                        bonusPoints,
                        $"{bonusEffect.CardId} bonus tại booth {context.BoothId.Value:D}",
                        cancellationToken);
                    boothResult.ScoreAdjustments.Add(
                        new PluginScoreAdjustment(context.TeamId, bonusPoints));
                    finalAwardedPoints += bonusPoints;
                }

                resolutions.Add(new CardEffectResolution(
                    bonusEffect.Id,
                    qualified ? "bonus_applied" : "qualification_not_met",
                    new BsonDocument
                    {
                        ["boothId"] = context.BoothId.Value.ToString(),
                        ["boothCompletionId"] = boothResult.BoothCompletionId.ToString(),
                        ["boothType"] = boothResult.BoothType,
                        ["boothResult"] = boothResult.Result,
                        ["submittedPoints"] = boothResult.SubmittedPoints,
                        ["boothMaximumScore"] = boothResult.BoothMaximumScore.HasValue
                            ? boothResult.BoothMaximumScore.Value
                            : BsonNull.Value,
                        ["qualified"] = qualified,
                        ["bonusPoints"] = bonusPoints,
                        ["finalAwardedPoints"] = finalAwardedPoints,
                        ["resolvedByEventId"] = context.EventId
                    }));
            }

            boothResult.FinalAwardedPoints = finalAwardedPoints;

            if (firewallEffect is not null)
            {
                var wasAttacked = firewallEffect.Data.TryGetValue("wasAttacked", out var attacked) &&
                                  attacked.IsBoolean && attacked.AsBoolean;
                var firewallBonus = wasAttacked
                    ? 0
                    : firewallEffect.Data.GetInt("bonusPoints", 25);
                if (firewallBonus > 0)
                {
                    await AdjustScoreAsync(
                        context,
                        context.TeamId,
                        firewallBonus,
                        $"Firewall bảo vệ thành công tại booth {context.BoothId.Value:D}",
                        cancellationToken,
                        $"{context.EventId}:firewall");
                    boothResult.ScoreAdjustments.Add(
                        new PluginScoreAdjustment(context.TeamId, firewallBonus));
                }

                resolutions.Add(new CardEffectResolution(
                    firewallEffect.Id,
                    wasAttacked ? "attack_blocked" : "protection_bonus_applied",
                    new BsonDocument
                    {
                        ["boothId"] = context.BoothId.Value.ToString(),
                        ["boothCompletionId"] = boothResult.BoothCompletionId.ToString(),
                        ["wasAttacked"] = wasAttacked,
                        ["bonusPoints"] = firewallBonus,
                        ["resolvedByEventId"] = context.EventId
                    },
                    context.OccurredAt.AddMinutes(
                        firewallEffect.Data.GetInt("timeBetweenUseMinutes", 20))));
            }

            foreach (var cupid in cupidEffects)
            {
                if (!Guid.TryParse(cupid.OwnerTeamId, out var ownerTeamId))
                    throw new ApplicationValidationException(
                        "Cupid effect có ownerTeamId không hợp lệ.");

                var succeeded = boothResult.Result == BoothResultValues.Succeeded;
                var ownerDelta = succeeded
                    ? checked((int)Math.Ceiling(
                        finalAwardedPoints * cupid.Data.GetDouble("rewardMultiplier", 1)))
                    : -cupid.Data.GetInt("failurePenalty", 5);
                if (ownerDelta != 0)
                {
                    await AdjustScoreAsync(
                        context,
                        ownerTeamId,
                        ownerDelta,
                        $"Cupid theo kết quả booth {context.BoothId.Value:D}",
                        cancellationToken);
                    boothResult.ScoreAdjustments.Add(
                        new PluginScoreAdjustment(ownerTeamId, ownerDelta));
                }

                var cooldownMinutes = cupid.Data.GetInt("timeBetweenUseMinutes", 15);
                resolutions.Add(new CardEffectResolution(
                    cupid.Id,
                    succeeded ? "target_succeeded" : "target_failed",
                    new BsonDocument
                    {
                        ["targetTeamId"] = context.TeamId.ToString(),
                        ["boothId"] = context.BoothId.Value.ToString(),
                        ["boothCompletionId"] = boothResult.BoothCompletionId.ToString(),
                        ["boothResult"] = boothResult.Result,
                        ["finalAwardedPoints"] = finalAwardedPoints,
                        ["ownerDelta"] = ownerDelta,
                        ["resolvedByEventId"] = context.EventId
                    },
                    context.OccurredAt.AddMinutes(cooldownMinutes)));
            }

            return new DeferredPluginEventExecution(
                token => repository.CompleteClaimedEffectsAsync(
                    context.RaceId,
                    context.Name,
                    context.EventId,
                    context.TeamId,
                    context.OccurredAt,
                    resolutions,
                    token),
                token => repository.ReleaseClaimedEffectsAsync(
                    context.RaceId,
                    context.EventId,
                    DateTime.UtcNow,
                    token));
        }
        catch
        {
            await repository.ReleaseClaimedEffectsAsync(
                context.RaceId,
                context.EventId,
                DateTime.UtcNow,
                CancellationToken.None);
            throw;
        }
    }

    private static bool IsBoothBonus(CardEffectDocument effect) =>
        effect.CardId is CardIds.Engineer or CardIds.Athlete;

    private static bool IsQualified(
        CardEffectDocument effect,
        BoothResultFinalizedData result)
    {
        if (result.Result != BoothResultValues.Succeeded) return false;
        return effect.Data.GetString("qualificationMode", "any_success") switch
        {
            "any_success" => true,
            "score_equals_booth_max" =>
                result.BoothMaximumScore.HasValue &&
                result.SubmittedPoints == result.BoothMaximumScore.Value,
            _ => throw new ApplicationValidationException(
                $"Qualification mode của {effect.CardId} không hợp lệ.")
        };
    }

    private static int CalculateBonus(int submittedPoints, double multiplier)
    {
        if (multiplier < 1)
            throw new ApplicationValidationException(
                "Score multiplier phải lớn hơn hoặc bằng 1.");
        return checked((int)Math.Ceiling(submittedPoints * (multiplier - 1)));
    }

    private async Task AdjustScoreAsync(
        PluginEventContext context,
        Guid teamId,
        int delta,
        string reason,
        CancellationToken cancellationToken,
        string? eventId = null)
    {
        var result = await sender.Send(new UpdateTeamScoreCommand
        {
            RaceId = context.RaceId,
            TeamId = teamId,
            EventId = eventId ?? context.EventId,
            Delta = delta,
            Reason = reason,
            PublishRealtimeNotification = false
        }, cancellationToken);
        if (result is null)
            throw new ApplicationValidationException(
                $"Không tìm thấy team '{teamId}' để áp dụng card.");
    }
}
