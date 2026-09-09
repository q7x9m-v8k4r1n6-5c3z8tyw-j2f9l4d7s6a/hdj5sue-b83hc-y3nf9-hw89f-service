using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class RaceCardServiceTests
{
    [Fact]
    public async Task GetAdminOverview_ReturnsMaximumStockForEveryCard()
    {
        var raceId = Guid.NewGuid();
        var repository = new InMemoryRaceCardRepository(new RaceCardDocument
        {
            Id = raceId.ToString(),
            RaceId = raceId.ToString(),
            Inventory = CardCatalog.All
                .Select(definition => new CardInventoryState
                {
                    CardId = definition.CardId,
                    RemainingStock = 2,
                    MaxStock = 5,
                    Price = (int)definition.Price,
                    CardConfig = definition.DefaultConfig.DeepClone().AsBsonDocument
                })
                .ToList()
        });
        var service = CreateService(repository, Guid.NewGuid());

        var response = await service.GetAdminOverviewAsync(raceId);

        Assert.NotEmpty(response.Cards);
        Assert.All(response.Cards, card => Assert.Equal(5, card.MaxStock));
    }

    [Fact]
    public async Task Restock_IncreasesAvailableAndMaximumStockTogether()
    {
        var raceId = Guid.NewGuid();
        var definition = CardCatalog.Get(CardIds.Engineer);
        var repository = new InMemoryRaceCardRepository(new RaceCardDocument
        {
            Id = raceId.ToString(),
            RaceId = raceId.ToString(),
            StoreOpen = false,
            Inventory =
            [
                new CardInventoryState
                {
                    CardId = definition.CardId,
                    RemainingStock = 2,
                    MaxStock = 5,
                    Price = (int)definition.Price,
                    CardConfig = definition.DefaultConfig.DeepClone().AsBsonDocument
                }
            ]
        });
        var service = CreateService(repository, Guid.NewGuid());

        await service.RestockAsync(
            raceId,
            new Dictionary<string, int> { [CardIds.Engineer] = 3 });

        var inventory = Assert.Single(repository.Document.Inventory);
        Assert.Equal(5, inventory.RemainingStock);
        Assert.Equal(8, inventory.MaxStock);
    }

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

    [Fact]
    public async Task DeleteAssignment_RejectsPurchasedCardAndDoesNotReturnStock()
    {
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var cardInstanceId = Guid.NewGuid();
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
            ],
            Teams =
            [
                new RaceCardTeamState
                {
                    TeamId = teamId.ToString(),
                    TeamName = "Team A",
                    Cards =
                    [
                        new TeamCardState
                        {
                            CardInfo = new TeamCardInfo
                            {
                                CardInstanceId = cardInstanceId.ToString(),
                                CardId = definition.CardId,
                                CardUseCountRemain = 1
                            },
                            ReceiveReason = "shop_purchase",
                            PurchaseId = Guid.NewGuid().ToString(),
                            PurchasePrice = 15,
                            Status = CardStatus.Received,
                            ReceivedAt = DateTime.UtcNow
                        }
                    ]
                }
            ]
        });
        var service = CreateService(repository, teamId);

        var exception = await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            service.DeleteAssignmentAsync(
                raceId,
                cardInstanceId,
                teamId,
                "admin cleanup",
                CancellationToken.None));

        Assert.Contains("hoàn tiền", exception.Message);
        Assert.Equal(CardStatus.Received,
            Assert.Single(Assert.Single(repository.Document.Teams).Cards).Status);
        Assert.Equal(1, Assert.Single(repository.Document.Inventory).RemainingStock);
    }

    private static RaceCardService CreateService(
        IRaceCardRepository repository,
        Guid teamId) =>
        new(
            repository,
            new CardUseHandlerResolver([]),
            null!,
            new InMemoryBoothRepository(new Booth()),
            new AssignedBoothOrganizerRepository(),
            new ValidBoothRaceRepository(),
            new StubTeamUserRepository(new User
            {
                Id = teamId,
                DisplayName = "Team A",
                Username = "team-a"
            }),
            NullLogger<RaceCardService>.Instance);

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
        public Task<bool> HasPendingReviveAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> GetPendingReviveAsync(Guid raceId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> ConfirmReviveAsync(Guid raceId, string effectId, Guid organizerId, DateTime confirmedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> GetEffectAsync(Guid raceId, string effectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveBoothResultEffectsAsync(Guid raceId, Guid teamId, DateTime occurredAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveEffectsByCardAsync(Guid raceId, string cardId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ClaimEffectsAsync(Guid raceId, IReadOnlyCollection<string> effectIds, string eventId, DateTime claimedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetClaimedEffectsAsync(Guid raceId, string eventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CompleteClaimedEffectsAsync(Guid raceId, string eventCode, string eventId, Guid triggeredByTeamId, DateTime resolvedAt, IReadOnlyCollection<CardEffectResolution> resolutions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseClaimedEffectsAsync(Guid raceId, string eventId, DateTime releasedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
