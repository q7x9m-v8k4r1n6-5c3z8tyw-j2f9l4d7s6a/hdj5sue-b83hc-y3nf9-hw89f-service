using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Repositories;

public interface IRaceCardRepository
{
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
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
    Task<CardEffectDocument?> TryClaimTrapAsync(
        Guid raceId,
        Guid boothId,
        Guid triggeringTeamId,
        DateTime triggeredAt,
        string resolvedByEventCode,
        string resolvedByEventId,
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
    Task ResolveEffectsAsync(
        Guid raceId,
        string eventCode,
        string eventId,
        Guid triggeredByTeamId,
        DateTime resolvedAt,
        IReadOnlyCollection<CardEffectResolution> resolutions,
        CancellationToken cancellationToken = default);
}
