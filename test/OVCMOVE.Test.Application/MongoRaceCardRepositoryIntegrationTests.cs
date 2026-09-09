using MongoDB.Bson;
using MongoDB.Driver;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class MongoRaceCardRepositoryIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetOrCreate_SeedsCanonicalShopCatalogAndPrices()
    {
        var connectionString = Environment.GetEnvironmentVariable("OVCMOVE_TEST_MONGODB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var client = new MongoClient(connectionString);
        var databaseName = $"ovc_test_{Guid.NewGuid():N}"[..33];
        var database = client.GetDatabase(databaseName);
        var raceCollection = database.GetCollection<RaceCardDocument>("race_cards");
        var effectCollection = database.GetCollection<CardEffectDocument>("effect");
        var raceId = Guid.NewGuid();

        try
        {
            var repository = new MongoRaceCardRepository(raceCollection, effectCollection);
            await repository.EnsureIndexesAsync();

            var document = await repository.GetOrCreateAsync(raceId);
            var secondRead = await repository.GetOrCreateAsync(raceId);

            Assert.False(document.StoreOpen);
            Assert.Equal(3, document.MaxDataPatchPerTeam);
            Assert.Equal(CardCatalog.All.Count, document.Inventory.Count);
            Assert.All(document.Inventory, item => Assert.True(item.Price >= 0));
            Assert.Equal(
                15,
                document.Inventory.Single(item => item.CardId == CardIds.Engineer).Price);
            Assert.Equal(document.Version, secondRead.Version);
            Assert.Equal(1, await raceCollection.CountDocumentsAsync(_ => true));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConfirmRevive_ConsumesCardAndResolvesPendingUseOnce()
    {
        var connectionString = Environment.GetEnvironmentVariable("OVCMOVE_TEST_MONGODB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var client = new MongoClient(connectionString);
        var databaseName = $"ovc_test_{Guid.NewGuid():N}"[..33];
        var database = client.GetDatabase(databaseName);
        var raceCollection = database.GetCollection<RaceCardDocument>("race_cards");
        var effectCollection = database.GetCollection<CardEffectDocument>("effect");
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var boothId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var cardInstanceId = Guid.NewGuid().ToString();
        var cardUseId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var effect = new CardEffectDocument
        {
            RaceId = raceId.ToString(),
            CardId = CardIds.Revive,
            CardInstanceId = cardInstanceId,
            CardUseId = cardUseId,
            OwnerTeamId = teamId.ToString(),
            TargetTeamId = teamId.ToString(),
            TargetBoothId = boothId.ToString(),
            TriggerEventCode = CardEffectEventCodes.ReviveOperatorConfirmation,
            Status = CardEffectStatus.Active,
            StartAt = now,
            CreatedAt = now,
            CreatedBy = teamId.ToString(),
            ModifiedAt = now,
            ModifiedBy = teamId.ToString()
        };
        var raceDocument = new RaceCardDocument
        {
            Id = raceId.ToString(),
            RaceId = raceId.ToString(),
            ModifiedAt = now,
            Teams =
            [
                new RaceCardTeamState
                {
                    TeamId = teamId.ToString(),
                    TeamName = "Revive Team",
                    Cards =
                    [
                        new TeamCardState
                        {
                            CardInfo = new TeamCardInfo
                            {
                                CardInstanceId = cardInstanceId,
                                CardId = CardIds.Revive,
                                CardUseCountRemain = 1
                            },
                            Status = CardStatus.Received,
                            ReceivedAt = now,
                            CardUses =
                            [
                                new CardUseState
                                {
                                    Id = cardUseId,
                                    EffectId = effect.Id,
                                    Status = CardUseStatus.Pending,
                                    UseAt = now,
                                    CardUseCountBefore = 1,
                                    CardUseCountAfter = 1
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        try
        {
            await raceCollection.InsertOneAsync(raceDocument);
            await effectCollection.InsertOneAsync(effect);
            var repository = new MongoRaceCardRepository(raceCollection, effectCollection);

            var pending = await repository.GetPendingReviveAsync(raceId, boothId, teamId);

            var resolved = await repository.ConfirmReviveAsync(
                raceId,
                effect.Id,
                organizerId,
                now.AddMinutes(1));
            var duplicate = await repository.ConfirmReviveAsync(
                raceId,
                effect.Id,
                organizerId,
                now.AddMinutes(2));

            Assert.Equal(effect.Id, pending?.Id);
            Assert.NotNull(resolved);
            Assert.Null(duplicate);
            Assert.Null(await repository.GetPendingReviveAsync(raceId, boothId, teamId));
            var storedRace = await raceCollection.Find(item => item.Id == raceId.ToString()).SingleAsync();
            var storedCard = Assert.Single(Assert.Single(storedRace.Teams).Cards);
            var storedUse = Assert.Single(storedCard.CardUses);
            var storedEffect = await effectCollection.Find(item => item.Id == effect.Id).SingleAsync();
            Assert.Equal(0, storedCard.CardInfo.CardUseCountRemain);
            Assert.Equal(CardStatus.Used, storedCard.Status);
            Assert.Equal(CardUseStatus.Resolved, storedUse.Status);
            Assert.Equal(CardEffectResolutionCodes.OperatorConfirmed, storedUse.Result?["decision"].AsString);
            Assert.Equal(CardEffectResolutionCodes.OperatorConfirmed, storedEffect.Resolution);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResolveEffects_UpgradesLegacyRaceDocumentWithoutVersion()
    {
        var connectionString = Environment.GetEnvironmentVariable("OVCMOVE_TEST_MONGODB");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var client = new MongoClient(connectionString);
        var databaseName = $"ovc_test_{Guid.NewGuid():N}"[..33];
        var database = client.GetDatabase(databaseName);
        var raceCollection = database.GetCollection<RaceCardDocument>("race_cards");
        var effectCollection = database.GetCollection<CardEffectDocument>("effect");
        var rawRaceCollection = database.GetCollection<BsonDocument>("race_cards");
        var rawEffectCollection = database.GetCollection<BsonDocument>("effect");

        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var cardInstanceId = Guid.NewGuid().ToString();
        var cardUseId = Guid.NewGuid().ToString();
        var effectId = ObjectId.GenerateNewId();
        var now = DateTime.UtcNow;

        try
        {
            await rawRaceCollection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = raceId.ToString(),
                ["raceid"] = raceId.ToString(),
                ["inventory"] = new BsonArray(),
                ["teams"] = new BsonArray
                {
                    new BsonDocument
                    {
                        ["teamId"] = teamId.ToString(),
                        ["teamName"] = "Legacy Team",
                        ["card"] = new BsonArray
                        {
                            new BsonDocument
                            {
                                ["cardInfo"] = new BsonDocument
                                {
                                    ["cardInstanceId"] = cardInstanceId,
                                    ["cardId"] = CardIds.Cupid,
                                    ["card_use_count_remain"] = 2
                                },
                                ["cardUse"] = new BsonArray
                                {
                                    new BsonDocument
                                    {
                                        ["id"] = cardUseId,
                                        ["effectId"] = effectId.ToString(),
                                        ["status"] = CardUseStatus.Active,
                                        ["inputs"] = new BsonDocument(),
                                        ["useAt"] = now,
                                        ["card_use_count_before"] = 3,
                                        ["card_use_count_after"] = 2
                                    }
                                },
                                ["receivedAt"] = now,
                                ["receiveReason"] = "legacy_seed",
                                ["status"] = CardStatus.Received
                            }
                        }
                    }
                },
                ["modifiedAt"] = now
            });
            await rawEffectCollection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = effectId,
                ["raceId"] = raceId.ToString(),
                ["cardId"] = CardIds.Cupid,
                ["cardInstanceId"] = cardInstanceId,
                ["cardUseId"] = cardUseId,
                ["ownerTeamId"] = Guid.NewGuid().ToString(),
                ["targetTeamId"] = teamId.ToString(),
                ["triggerEventCode"] = CardEffectEventCodes.BoothResultFinalized,
                ["status"] = CardEffectStatus.Active,
                ["remainingTriggers"] = 1,
                ["startAt"] = now,
                ["data"] = new BsonDocument(),
                ["createdAt"] = now,
                ["createdBy"] = "legacy_seed",
                ["modifiedAt"] = now,
                ["modifiedBy"] = "legacy_seed"
            });

            var repository = new MongoRaceCardRepository(raceCollection, effectCollection);
            var eventId = $"booth-result:{Guid.NewGuid():D}";
            await repository.ClaimEffectsAsync(
                raceId,
                [effectId.ToString()],
                eventId,
                now.AddMinutes(1),
                CancellationToken.None);

            var claimedEffect = await rawEffectCollection
                .Find(new BsonDocument("_id", effectId))
                .SingleAsync();
            var raceBeforeCompletion = await rawRaceCollection
                .Find(new BsonDocument("_id", raceId.ToString()))
                .SingleAsync();
            Assert.Equal(CardEffectStatus.Active, claimedEffect["status"].AsString);
            Assert.Equal(eventId, claimedEffect["claimedByEventId"].AsString);
            Assert.Equal(
                CardUseStatus.Active,
                raceBeforeCompletion["teams"][0]["card"][0]["cardUse"][0]["status"].AsString);

            await repository.CompleteClaimedEffectsAsync(
                raceId,
                CardEffectEventCodes.BoothResultFinalized,
                eventId,
                teamId,
                now.AddMinutes(1),
                [new CardEffectResolution(effectId.ToString(), "succeeded", new BsonDocument("awardedPoints", 20))],
                CancellationToken.None);
            await repository.CompleteClaimedEffectsAsync(
                raceId,
                CardEffectEventCodes.BoothResultFinalized,
                eventId,
                teamId,
                now.AddMinutes(1),
                [new CardEffectResolution(effectId.ToString(), "succeeded", new BsonDocument("awardedPoints", 20))],
                CancellationToken.None);

            var storedRace = await rawRaceCollection.Find(new BsonDocument("_id", raceId.ToString())).SingleAsync();
            var storedEffect = await rawEffectCollection.Find(new BsonDocument("_id", effectId)).SingleAsync();
            var storedUse = storedRace["teams"][0]["card"][0]["cardUse"][0].AsBsonDocument;

            Assert.Equal(1, storedRace["version"].ToInt64());
            Assert.Equal(CardUseStatus.Resolved, storedUse["status"].AsString);
            // Claim and completion are two separate writes guarded by the effect version.
            Assert.Equal(2, storedEffect["version"].ToInt64());
            Assert.Equal(CardEffectStatus.Resolved, storedEffect["status"].AsString);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }
}
