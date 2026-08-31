using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Services;

public interface IRaceCardService
{
    Task<CardStoreOverviewResponse> GetAdminOverviewAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CardTeamResponse>> GetCardTeamsAsync(Guid raceId, string cardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeamCardResponse>> GetTeamCardsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamCardResponse> GetTeamCardAsync(Guid raceId, Guid teamId, string cardId, CancellationToken cancellationToken = default);
    Task SetStoreOpenAsync(Guid raceId, bool isOpen, CancellationToken cancellationToken = default);
    Task RestockAsync(Guid raceId, IReadOnlyDictionary<string, int> quantities, CancellationToken cancellationToken = default);
    Task ScheduleRestockAsync(Guid raceId, DateTime scheduledAt, IReadOnlyDictionary<string, int> quantities, CancellationToken cancellationToken = default);
    Task UpdateConfigAsync(Guid raceId, string cardId, IReadOnlyDictionary<string, string> config, CancellationToken cancellationToken = default);
    Task<CardTeamResponse> AssignAsync(Guid raceId, string cardId, Guid teamId, string teamName, string reason, CancellationToken cancellationToken = default);
    Task DeleteAssignmentAsync(Guid raceId, string cardId, Guid teamId, string reason, CancellationToken cancellationToken = default);
    Task<CardUseResponse> UseAsync(Guid raceId, Guid teamId, string cardId, IReadOnlyDictionary<string, string> inputs, CancellationToken cancellationToken = default);
    Task<TrapState?> TriggerTrapAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default);
}
