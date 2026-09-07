using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public interface ICardEventReconciliationService
{
    Task<CardEventReconciliationResponse> ReconcileAsync(
        Guid raceId,
        string eventId,
        Guid actorId,
        CancellationToken cancellationToken = default);
}

public sealed record CardEventReconciliationResponse(
    string EventId,
    int ScoringLogCount,
    int CompletedEffectCount,
    bool OverclockWindowResolved);

/// <summary>
/// Completes Mongo state after SQL is known to have committed. This operation
/// never reapplies a score; the SQL scoring log is the commit proof.
/// </summary>
public sealed class CardEventReconciliationService(
    IRaceCardRepository cardRepository,
    IRaceRepository raceRepository,
    ILogger<CardEventReconciliationService> logger) : ICardEventReconciliationService
{
    public async Task<CardEventReconciliationResponse> ReconcileAsync(
        Guid raceId,
        string eventId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (raceId == Guid.Empty || actorId == Guid.Empty)
            throw new ApplicationValidationException("RaceId và actorId là bắt buộc.");
        if (string.IsNullOrWhiteSpace(eventId) || eventId.Length > 200)
            throw new ApplicationValidationException(
                "EventId phải có từ 1 đến 200 ký tự.");

        eventId = eventId.Trim();
        var scoringLogs = await raceRepository.GetScoringLogsByEventIdAsync(
            raceId,
            eventId,
            cancellationToken);
        if (scoringLogs.Count == 0)
            throw new ApplicationConflictException(
                "Không tìm thấy scoring log đã commit cho event này; không thể reconcile an toàn.");

        var claimedEffects = await cardRepository.GetClaimedEffectsAsync(
            raceId,
            eventId,
            cancellationToken);
        if (claimedEffects.Count > 0)
        {
            var eventCodes = claimedEffects
                .Select(item => item.TriggerEventCode)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (eventCodes.Length != 1)
                throw new ApplicationConflictException(
                    "Các effect được claim không cùng một event code.");

            var reconciledAt = DateTime.UtcNow;
            var scoringLogIds = new BsonArray(
                scoringLogs.Select(item => item.Id.ToString()));
            var resolutions = claimedEffects.Select(effect =>
                new CardEffectResolution(
                    effect.Id,
                    "reconciled_after_sql_commit",
                    new BsonDocument
                    {
                        ["reconciled"] = true,
                        ["reconciledAt"] = reconciledAt,
                        ["resolvedByEventId"] = eventId,
                        ["scoringLogIds"] = scoringLogIds.DeepClone()
                    },
                    GetNextTimeAvailable(effect))).ToArray();
            var triggeringTeamId = scoringLogs
                .FirstOrDefault(item =>
                    item.EventCode == ScoringLogConstants.EventCode.Booth)?.TeamId
                ?? actorId;

            await cardRepository.CompleteClaimedEffectsAsync(
                raceId,
                eventCodes[0],
                eventId,
                triggeringTeamId,
                reconciledAt,
                resolutions,
                cancellationToken);
        }

        var overclockWindowResolved = await TryResolveOverclockWindowAsync(
            raceId,
            eventId,
            cancellationToken);
        logger.LogInformation(
            "Reconciled card event {EventId}: {EffectCount} effect(s), {ScoringLogCount} scoring log(s).",
            eventId,
            claimedEffects.Count,
            scoringLogs.Count);

        return new CardEventReconciliationResponse(
            eventId,
            scoringLogs.Count,
            claimedEffects.Count,
            overclockWindowResolved);
    }

    private async Task<bool> TryResolveOverclockWindowAsync(
        Guid raceId,
        string eventId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var document = await cardRepository.GetOrCreateAsync(raceId, cancellationToken);
            var window = document.OverclockWindow;
            if (window.ResolutionEventId != eventId)
                return false;
            if (window.Status == OverclockWindowStatus.Resolved)
                return true;
            if (window.Status != OverclockWindowStatus.Closed)
                throw new ApplicationConflictException(
                    "Trạng thái Overclock không hợp lệ để reconcile.");

            window.Status = OverclockWindowStatus.Resolved;
            window.ResolvedAt = DateTime.UtcNow;
            try
            {
                await cardRepository.ReplaceAsync(document, cancellationToken);
                return true;
            }
            catch (ApplicationConflictException) when (attempt < 3)
            {
                // Reload the latest document and retry optimistic concurrency.
            }
        }

        throw new ApplicationConflictException(
            "Không thể cập nhật trạng thái Overclock sau ba lần thử.");
    }

    private static DateTime? GetNextTimeAvailable(CardEffectDocument effect) =>
        effect.CardId == CardIds.Cupid
            ? (effect.ClaimedAt ?? DateTime.UtcNow)
                .AddMinutes(effect.Data.GetInt("timeBetweenUseMinutes", 15))
            : null;
}
