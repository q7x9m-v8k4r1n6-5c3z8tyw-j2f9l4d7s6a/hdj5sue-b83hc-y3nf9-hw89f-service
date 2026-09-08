using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Entities;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class CardShopServiceTests
{
    [Fact]
    public async Task Purchase_DebitsScoreReservesStockAndAssignsCard()
    {
        var fixture = CreateFixture(score: 40, stock: 2, storeOpen: true);
        var purchaseId = Guid.NewGuid();

        var result = await fixture.Service.PurchaseAsync(
            fixture.RaceId,
            fixture.TeamId,
            CardIds.Engineer,
            purchaseId);

        Assert.Equal(25, fixture.RaceRepository.Score);
        Assert.Equal(25, result.ScoreAfter);
        Assert.Equal(CardStatus.Received, result.Status);
        Assert.Equal(1, Assert.Single(fixture.CardRepository.Document.Inventory).RemainingStock);
        var card = Assert.Single(Assert.Single(fixture.CardRepository.Document.Teams).Cards);
        Assert.Equal(purchaseId.ToString(), card.PurchaseId);
        Assert.Equal(15, card.PurchasePrice);
        Assert.Equal(CardStatus.Received, card.Status);
        Assert.Equal(-15, Assert.Single(fixture.RaceRepository.Logs).Delta);
        Assert.Equal(1, fixture.UnitOfWork.CommitCount);
        Assert.Single(fixture.Notifications.ScoreChanges);
    }

    [Fact]
    public async Task Purchase_RetryWithSameIdDoesNotDebitOrAssignTwice()
    {
        var fixture = CreateFixture(score: 40, stock: 2, storeOpen: true);
        var purchaseId = Guid.NewGuid();

        var first = await fixture.Service.PurchaseAsync(
            fixture.RaceId, fixture.TeamId, CardIds.Engineer, purchaseId);
        var second = await fixture.Service.PurchaseAsync(
            fixture.RaceId, fixture.TeamId, CardIds.Engineer, purchaseId);

        Assert.Equal(first.CardInstanceId, second.CardInstanceId);
        Assert.Equal(25, fixture.RaceRepository.Score);
        Assert.Single(fixture.RaceRepository.Logs);
        Assert.Single(Assert.Single(fixture.CardRepository.Document.Teams).Cards);
        Assert.Equal(1, Assert.Single(fixture.CardRepository.Document.Inventory).RemainingStock);
        Assert.Equal(1, fixture.UnitOfWork.CommitCount);
        Assert.Single(fixture.Notifications.ScoreChanges);
    }

    [Fact]
    public async Task Purchase_RetryCompletesPendingMongoWhenSqlLogAlreadyExists()
    {
        var fixture = CreateFixture(score: 40, stock: 2, storeOpen: true);
        var purchaseId = Guid.NewGuid();
        await fixture.Service.PurchaseAsync(
            fixture.RaceId, fixture.TeamId, CardIds.Engineer, purchaseId);
        var interruptedDocument = fixture.CardRepository.Document;
        Assert.Single(Assert.Single(interruptedDocument.Teams).Cards).Status =
            CardStatus.PendingPurchase;
        await fixture.CardRepository.ReplaceAsync(interruptedDocument);

        var recovered = await fixture.Service.PurchaseAsync(
            fixture.RaceId, fixture.TeamId, CardIds.Engineer, purchaseId);

        Assert.Equal(CardStatus.Received, recovered.Status);
        Assert.Equal(CardStatus.Received,
            Assert.Single(Assert.Single(fixture.CardRepository.Document.Teams).Cards).Status);
        Assert.Equal(25, fixture.RaceRepository.Score);
        Assert.Single(fixture.RaceRepository.Logs);
        Assert.Equal(1, fixture.UnitOfWork.CommitCount);
    }

    [Fact]
    public async Task Purchase_WhenMongoCompletionFails_RetryDoesNotChargeTwice()
    {
        var fixture = CreateFixture(score: 40, stock: 2, storeOpen: true);
        var purchaseId = Guid.NewGuid();
        fixture.CardRepository.FailReceivedReplacementCount = 3;

        var interrupted = await fixture.Service.PurchaseAsync(
            fixture.RaceId, fixture.TeamId, CardIds.Engineer, purchaseId);

        Assert.Equal(CardStatus.PendingPurchase, interrupted.Status);
        Assert.Equal(25, fixture.RaceRepository.Score);
        Assert.Single(fixture.RaceRepository.Logs);
        Assert.Equal(CardStatus.PendingPurchase,
            Assert.Single(Assert.Single(fixture.CardRepository.Document.Teams).Cards).Status);

        var recovered = await fixture.Service.PurchaseAsync(
            fixture.RaceId, fixture.TeamId, CardIds.Engineer, purchaseId);

        Assert.Equal(CardStatus.Received, recovered.Status);
        Assert.Equal(25, fixture.RaceRepository.Score);
        Assert.Single(fixture.RaceRepository.Logs);
        Assert.Single(Assert.Single(fixture.CardRepository.Document.Teams).Cards);
        Assert.Equal(1, fixture.UnitOfWork.CommitCount);
        Assert.Single(fixture.Notifications.ScoreChanges);
    }

    [Fact]
    public async Task Purchase_WhenScoreIsInsufficientReleasesReservation()
    {
        var fixture = CreateFixture(score: 10, stock: 2, storeOpen: true);

        var exception = await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            fixture.Service.PurchaseAsync(
                fixture.RaceId,
                fixture.TeamId,
                CardIds.Engineer,
                Guid.NewGuid()));

        Assert.Contains("không đủ CD", exception.Message);
        Assert.Equal(10, fixture.RaceRepository.Score);
        Assert.Empty(fixture.RaceRepository.Logs);
        Assert.Empty(Assert.Single(fixture.CardRepository.Document.Teams).Cards);
        Assert.Equal(2, Assert.Single(fixture.CardRepository.Document.Inventory).RemainingStock);
        Assert.Equal(1, fixture.UnitOfWork.RollbackCount);
    }

    [Fact]
    public async Task Purchase_WhenStoreIsClosedDoesNotReserveStock()
    {
        var fixture = CreateFixture(score: 40, stock: 2, storeOpen: false);

        await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            fixture.Service.PurchaseAsync(
                fixture.RaceId,
                fixture.TeamId,
                CardIds.Engineer,
                Guid.NewGuid()));

        Assert.Equal(40, fixture.RaceRepository.Score);
        Assert.Empty(fixture.CardRepository.Document.Teams);
        Assert.Equal(2, Assert.Single(fixture.CardRepository.Document.Inventory).RemainingStock);
    }

    [Fact]
    public async Task Purchase_UsedDataPatchStillCountsTowardRaceLimit()
    {
        var fixture = CreateFixture(score: 40, stock: 2, storeOpen: true);
        var document = fixture.CardRepository.Document;
        document.MaxDataPatchPerTeam = 1;
        document.Teams.Add(new RaceCardTeamState
        {
            TeamId = fixture.TeamId.ToString(),
            TeamName = "Team A",
            Cards =
            [
                new TeamCardState
                {
                    CardInfo = new TeamCardInfo
                    {
                        CardInstanceId = Guid.NewGuid().ToString(),
                        CardId = CardIds.Athlete,
                        CardUseCountRemain = 0
                    },
                    ReceiveReason = "shop_purchase",
                    Status = CardStatus.Used,
                    ReceivedAt = DateTime.UtcNow.AddMinutes(-10)
                }
            ]
        });
        await fixture.CardRepository.ReplaceAsync(document);

        await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            fixture.Service.PurchaseAsync(
                fixture.RaceId,
                fixture.TeamId,
                CardIds.Engineer,
                Guid.NewGuid()));

        Assert.Equal(40, fixture.RaceRepository.Score);
        Assert.Equal(2, Assert.Single(fixture.CardRepository.Document.Inventory).RemainingStock);
    }

    [Fact]
    public async Task SetPrice_RejectsCoreChipAndChangesDataPatchPriceWhileClosed()
    {
        var fixture = CreateFixture(score: 40, stock: 2, storeOpen: false);

        var updated = await fixture.Service.SetPriceAsync(
            fixture.RaceId, CardIds.Engineer, 25);

        Assert.Equal(25, updated.Price);
        Assert.Equal(25, Assert.Single(fixture.CardRepository.Document.Inventory).Price);
        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            fixture.Service.SetPriceAsync(fixture.RaceId, CardIds.Cupid, 25));
    }

    private static Fixture CreateFixture(int score, int stock, bool storeOpen)
    {
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var definition = CardCatalog.Get(CardIds.Engineer);
        var cardRepository = new ShopCardRepository(new RaceCardDocument
        {
            Id = raceId.ToString(),
            RaceId = raceId.ToString(),
            StoreOpen = storeOpen,
            MaxDataPatchPerTeam = 3,
            Inventory =
            [
                new CardInventoryState
                {
                    CardId = definition.CardId,
                    Price = 15,
                    RemainingStock = stock,
                    CardConfig = definition.DefaultConfig.DeepClone().AsBsonDocument
                }
            ]
        });
        var raceRepository = new ShopRaceRepository(score);
        var unitOfWork = new UnitOfWorkSpy();
        var notifications = new BoothNotificationSpy();
        var service = new CardShopService(
            cardRepository,
            raceRepository,
            new StubTeamUserRepository(new User
            {
                Id = teamId,
                DisplayName = "Team A",
                Username = "team-a"
            }),
            unitOfWork,
            notifications,
            NullLogger<CardShopService>.Instance);
        return new Fixture(
            raceId,
            teamId,
            service,
            cardRepository,
            raceRepository,
            unitOfWork,
            notifications);
    }

    private sealed record Fixture(
        Guid RaceId,
        Guid TeamId,
        CardShopService Service,
        ShopCardRepository CardRepository,
        ShopRaceRepository RaceRepository,
        UnitOfWorkSpy UnitOfWork,
        BoothNotificationSpy Notifications);

    private sealed class ShopCardRepository(RaceCardDocument document) : IRaceCardRepository
    {
        private RaceCardDocument _document = Clone(document);
        public RaceCardDocument Document => Clone(_document);
        public int FailReceivedReplacementCount { get; set; }

        public Task<RaceCardDocument> GetOrCreateAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Clone(_document));
        public Task ReplaceAsync(RaceCardDocument replacement, CancellationToken cancellationToken = default)
        {
            if (FailReceivedReplacementCount > 0 && replacement.Teams
                    .SelectMany(team => team.Cards)
                    .Any(card => card.PurchaseId is not null && card.Status == CardStatus.Received))
            {
                FailReceivedReplacementCount--;
                throw new ApplicationConflictException("Simulated Mongo completion conflict.");
            }
            _document = Clone(replacement);
            return Task.CompletedTask;
        }
        public Task EnsureIndexesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReplaceWithEffectAsync(RaceCardDocument replacement, CardEffectDocument effect, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasActiveTrapAsync(Guid raceId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> TryClaimTrapAsync(Guid raceId, Guid boothId, Guid triggeringTeamId, DateTime triggeredAt, string resolvedByEventCode, string resolvedByEventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPendingReviveAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> ResolveReviveAsync(Guid raceId, string effectId, Guid organizerId, string resolution, DateTime confirmedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> GetEffectAsync(Guid raceId, string effectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveBoothResultEffectsAsync(Guid raceId, Guid teamId, DateTime occurredAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveEffectsByCardAsync(Guid raceId, string cardId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ClaimEffectsAsync(Guid raceId, IReadOnlyCollection<string> effectIds, string eventId, DateTime claimedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetClaimedEffectsAsync(Guid raceId, string eventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CompleteClaimedEffectsAsync(Guid raceId, string eventCode, string eventId, Guid triggeredByTeamId, DateTime resolvedAt, IReadOnlyCollection<CardEffectResolution> resolutions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReleaseClaimedEffectsAsync(Guid raceId, string eventId, DateTime releasedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private static RaceCardDocument Clone(RaceCardDocument value) =>
            BsonSerializer.Deserialize<RaceCardDocument>(value.ToBsonDocument());
    }

    private sealed class ShopRaceRepository(int score) : IRaceRepository
    {
        public int Score { get; private set; } = score;
        public List<ScoringLog> Logs { get; } = [];

        public Task<RaceTeamScoreMutation?> TryDebitRaceTeamScoreAsync(Guid raceId, Guid teamId, int amount, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default)
        {
            if (Score < amount) return Task.FromResult<RaceTeamScoreMutation?>(null);
            var before = Score;
            Score -= amount;
            return Task.FromResult<RaceTeamScoreMutation?>(new(before, Score));
        }

        public Task CreateScoringLogAsync(ScoringLog log, CancellationToken cancellationToken = default)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ScoringLog>> GetScoringLogsByEventIdAsync(Guid raceId, string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ScoringLog>>(Logs.Where(log => log.RaceId == raceId && log.EventId == eventId).ToArray());
        public Task<bool> IsTeamInRaceAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task CreateAsync(Race race, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(RacePageRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Race race, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<BoothListResultModel>> GetBoothListAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<ScoringLogResultModel> Items, int TotalItems)> GetScoringLogPageByRaceIdAsync(Guid raceId, Guid? teamId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int CompletedRegularBooths, int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetRaceTeamScoreAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult<int?>(Score);
        public Task<bool> UpdateRaceTeamScoreAsync(Guid raceId, Guid teamId, int totalScore, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateRaceMessageAsync(RaceMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(Guid raceId, int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BoothProgressResultModel> GetBoothProgressAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<FinalizedBoothOutcome>> GetFinalizedBoothOutcomesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
