using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Repositories;

public interface IRaceCardRepository
{
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
        CancellationToken cancellationToken = default);
    Task<int> ApplyDueRestocksAsync(DateTime now, CancellationToken cancellationToken = default);
}
