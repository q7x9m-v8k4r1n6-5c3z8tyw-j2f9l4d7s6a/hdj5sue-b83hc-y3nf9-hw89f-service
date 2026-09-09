using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Repositories;

public interface IRaceCardRepository
{
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
    Task ExpireTimedEffectsAsync(
        Guid raceId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);
    Task<RaceCardDocument> GetOrCreateAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task ReplaceAsync(RaceCardDocument document, CancellationToken cancellationToken = default);
    Task ReplaceWithEffectAsync(
        RaceCardDocument document,
        CardEffectDocument effect,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveTrapAsync(
        Guid raceId,
        Guid boothId,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveBoothEffectAsync(
        Guid raceId,
        Guid boothId,
        string cardId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);
    Task<CardEffectDocument?> TryClaimTrapAsync(
        Guid raceId,
        Guid boothId,
        Guid triggeringTeamId,
        DateTime triggeredAt,
        string resolvedByEventCode,
        string resolvedByEventId,
        CancellationToken cancellationToken = default);
    Task<CardEffectDocument?> TryClaimTaxmanAsync(
        Guid raceId,
        Guid boothId,
        Guid triggeringTeamId,
        DateTime triggeredAt,
        string resolvedByEventCode,
        string resolvedByEventId,
        CancellationToken cancellationToken = default);
    Task<bool> HasPendingReviveAsync(
        Guid raceId,
        Guid teamId,
        Guid boothId,
        CancellationToken cancellationToken = default);
    Task<CardEffectDocument?> GetPendingReviveAsync(
        Guid raceId,
        Guid boothId,
        CancellationToken cancellationToken = default);
    Task<CardEffectDocument?> ConfirmReviveAsync(
        Guid raceId,
        string effectId,
        Guid organizerId,
        DateTime confirmedAt,
        CancellationToken cancellationToken = default);
    Task<CardEffectDocument?> GetEffectAsync(
        Guid raceId,
        string effectId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CardEffectDocument>> GetActiveBoothResultEffectsAsync(
        Guid raceId,
        Guid teamId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CardEffectDocument>> GetActiveEffectsByCardAsync(
        Guid raceId,
        string cardId,
        CancellationToken cancellationToken = default);
    Task ClaimEffectsAsync(
        Guid raceId,
        IReadOnlyCollection<string> effectIds,
        string eventId,
        DateTime claimedAt,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CardEffectDocument>> GetClaimedEffectsAsync(
        Guid raceId,
        string eventId,
        CancellationToken cancellationToken = default);
    Task CompleteClaimedEffectsAsync(
        Guid raceId,
        string eventCode,
        string eventId,
        Guid triggeredByTeamId,
        DateTime resolvedAt,
        IReadOnlyCollection<CardEffectResolution> resolutions,
        CancellationToken cancellationToken = default);
    Task ReleaseClaimedEffectsAsync(
        Guid raceId,
        string eventId,
        DateTime releasedAt,
        CancellationToken cancellationToken = default);
}
