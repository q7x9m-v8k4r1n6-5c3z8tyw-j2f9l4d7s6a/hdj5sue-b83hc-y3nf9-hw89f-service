using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using OVCMOVE.Domain.Entities;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class RaceCardServiceTests
{
    [Fact]
    public async Task Assign_UsesCanonicalTeamNameFromSqlUser()
    {
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var definition = CardCatalog.Get(CardIds.Engineer);
        var repository = new InMemoryRaceCardRepository(new RaceCardDocument
        {
            Id = raceId.ToString(),
            RaceId = raceId.ToString(),
            Inventory =
            [
                new CardInventoryState
                {
                    CardId = definition.CardId,
                    RemainingStock = 1,
                    CardConfig = definition.DefaultConfig.DeepClone().AsBsonDocument
                }
            ]
        });
        var user = new User
        {
            Id = teamId,
            DisplayName = "  Tên đội từ SQL  ",
            Username = "ten-tu-request-khong-duoc-dung",
            LinkedEmail = "team@example.test"
        };
        var service = new RaceCardService(
            repository,
            new CardUseHandlerResolver([]),
            null!,
            new InMemoryBoothRepository(new Booth()),
            new AssignedBoothOrganizerRepository(),
            new ValidBoothRaceRepository(),
            new StubTeamUserRepository(user),
            NullLogger<RaceCardService>.Instance);

        var response = await service.AssignAsync(
            raceId,
            CardIds.Engineer,
            teamId,
            "admin_assign",
            CancellationToken.None);

        Assert.Equal("Tên đội từ SQL", response.TeamName);
        Assert.Equal("Tên đội từ SQL", Assert.Single(repository.Document.Teams).TeamName);
        Assert.Equal(0, Assert.Single(repository.Document.Inventory).RemainingStock);
    }

    private sealed class InMemoryRaceCardRepository(RaceCardDocument document)
        : IRaceCardRepository
    {
        public RaceCardDocument Document { get; } = document;

        public Task<RaceCardDocument> GetOrCreateAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) => Task.FromResult(Document);

        public Task ReplaceAsync(
            RaceCardDocument replacement,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureIndexesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReplaceWithEffectAsync(RaceCardDocument document, CardEffectDocument effect, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasActiveTrapAsync(Guid raceId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> TryClaimTrapAsync(Guid raceId, Guid boothId, Guid triggeringTeamId, DateTime triggeredAt, string resolvedByEventCode, string resolvedByEventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> ConfirmReviveAsync(Guid raceId, string effectId, Guid organizerId, DateTime confirmedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> GetEffectAsync(Guid raceId, string effectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveBoothResultEffectsAsync(Guid raceId, Guid teamId, DateTime occurredAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ResolveEffectsAsync(Guid raceId, string eventCode, string eventId, Guid triggeredByTeamId, DateTime resolvedAt, IReadOnlyCollection<CardEffectResolution> resolutions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
