using OVCMOVE2026.Plugin.Models;
using System.Text.Json;
using MongoDB.Bson;

namespace OVCMOVE2026.Plugin.Services;

public interface IRaceCardService
{
    Task<CardStoreOverviewResponse> GetAdminOverviewAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CardTeamResponse>> GetCardTeamsAsync(Guid raceId, string cardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamCardResponse>> GetTeamCardsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamCardResponse> GetTeamCardAsync(Guid raceId, Guid teamId, Guid cardInstanceId, CancellationToken cancellationToken = default);
    Task RestockAsync(Guid raceId, IReadOnlyDictionary<string, int> quantities, CancellationToken cancellationToken = default);
    Task UpdateConfigAsync(Guid raceId, string cardId, IReadOnlyDictionary<string, JsonElement> config, CancellationToken cancellationToken = default);
    Task<CardTeamResponse> AssignAsync(Guid raceId, string cardId, Guid teamId, string teamName, string reason, CancellationToken cancellationToken = default);
    Task DeleteAssignmentAsync(Guid raceId, Guid cardInstanceId, Guid teamId, string reason, CancellationToken cancellationToken = default);
    Task<CardUseResponse> UseAsync(Guid raceId, Guid teamId, Guid cardInstanceId, Guid cardUseId, BsonDocument inputs, CancellationToken cancellationToken = default);
    Task ConfirmReviveAsync(Guid raceId, string effectId, Guid organizerId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<CardEffectDocument?> TriggerTrapAsync(
        Guid raceId,
        Guid boothId,
        Guid teamId,
        DateTime occurredAt,
        string eventCode,
        string eventId,
        CancellationToken cancellationToken = default);
}
