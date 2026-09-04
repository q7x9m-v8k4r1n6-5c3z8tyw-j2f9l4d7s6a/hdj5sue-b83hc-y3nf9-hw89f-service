using MongoDB.Driver;
using MongoDB.Bson;
using OVCMOVE.Application.Common;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE2026.Plugin.Repositories;

public sealed class MongoRaceCardRepository(
    IMongoCollection<RaceCardDocument> collection,
    IMongoCollection<CardEffectDocument> effectCollection) : IRaceCardRepository
{
    public async Task<RaceCardDocument> GetOrCreateAsync(
        Guid raceId,
        CancellationToken cancellationToken = default)
    {
        var raceKey = raceId.ToString();
        var document = await collection
            .Find(item => item.Id == raceKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            document = new RaceCardDocument
            {
                Id = raceKey,
                RaceId = raceKey,
                ModifiedAt = DateTime.UtcNow,
                Inventory = CardCatalog.All
                    .Select(card => new CardInventoryState
                    {
                        CardId = card.CardId,
                        CardConfig = ToBsonDocument(card.DefaultConfig)
                    })
                    .ToList()
            };

            try
            {
                await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
            }
            catch (MongoWriteException exception) when (
                exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                document = await collection
                    .Find(item => item.Id == raceKey)
                    .FirstAsync(cancellationToken);
            }
        }

        return document;
    }

    public async Task ReplaceAsync(
        RaceCardDocument document,
        CancellationToken cancellationToken = default)
    {
        var expectedVersion = document.Version;
        document.ModifiedAt = DateTime.UtcNow;
        document.Version = expectedVersion + 1;
        var versionFilter = expectedVersion == 0
            ? Builders<RaceCardDocument>.Filter.Or(
                Builders<RaceCardDocument>.Filter.Eq(item => item.Version, 0),
                Builders<RaceCardDocument>.Filter.Exists("version", false))
            : Builders<RaceCardDocument>.Filter.Eq(item => item.Version, expectedVersion);
        var filter = Builders<RaceCardDocument>.Filter.And(
            Builders<RaceCardDocument>.Filter.Eq(item => item.Id, document.Id),
            versionFilter);
        var result = await collection.ReplaceOneAsync(
            filter,
            document,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);

        if (result.IsAcknowledged && result.MatchedCount == 1) return;

        document.Version = expectedVersion;
        throw new ApplicationConflictException(
            "Dữ liệu card vừa được cập nhật bởi yêu cầu khác. Vui lòng tải lại và thử lại.");
    }

    public async Task ReplaceWithEffectAsync(
        RaceCardDocument document,
        CardEffectDocument effect,
        CancellationToken cancellationToken = default)
    {
        var expectedVersion = document.Version;
        document.ModifiedAt = DateTime.UtcNow;
        document.Version = expectedVersion + 1;
        effect.ModifiedAt = effect.CreatedAt;

        using var session = await collection.Database.Client.StartSessionAsync(
            cancellationToken: cancellationToken);
        session.StartTransaction();
        try
        {
            var versionFilter = expectedVersion == 0
                ? Builders<RaceCardDocument>.Filter.Or(
                    Builders<RaceCardDocument>.Filter.Eq(item => item.Version, 0),
                    Builders<RaceCardDocument>.Filter.Exists("version", false))
                : Builders<RaceCardDocument>.Filter.Eq(item => item.Version, expectedVersion);
            var filter = Builders<RaceCardDocument>.Filter.And(
                Builders<RaceCardDocument>.Filter.Eq(item => item.Id, document.Id),
                versionFilter);
            var result = await collection.ReplaceOneAsync(
                session,
                filter,
                document,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);
            if (!result.IsAcknowledged || result.MatchedCount != 1)
                throw new ApplicationConflictException(
                    "Dữ liệu card vừa được cập nhật bởi yêu cầu khác. Vui lòng tải lại và thử lại.");

            await effectCollection.InsertOneAsync(session, effect, cancellationToken: cancellationToken);
            await session.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            document.Version = expectedVersion;
            if (session.IsInTransaction)
                await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    public Task<bool> HasActiveTrapAsync(
        Guid raceId,
        Guid boothId,
        CancellationToken cancellationToken = default) =>
        effectCollection.Find(effect =>
                effect.RaceId == raceId.ToString() &&
                effect.CardId == CardIds.Trap &&
                effect.TargetBoothId == boothId.ToString() &&
                effect.Status == CardEffectStatus.Active)
            .AnyAsync(cancellationToken);

    public async Task<CardEffectDocument?> TryClaimTrapAsync(
        Guid raceId,
        Guid boothId,
        Guid triggeringTeamId,
        DateTime triggeredAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<CardEffectDocument>.Filter.And(
            Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.CardId, CardIds.Trap),
            Builders<CardEffectDocument>.Filter.Eq(item => item.TargetBoothId, boothId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
            Builders<CardEffectDocument>.Filter.Ne(item => item.OwnerTeamId, triggeringTeamId.ToString()),
            Builders<CardEffectDocument>.Filter.Or(
                Builders<CardEffectDocument>.Filter.Eq(item => item.LimitEndAt, null),
                Builders<CardEffectDocument>.Filter.Gt(item => item.LimitEndAt, triggeredAt)));
        var update = Builders<CardEffectDocument>.Update
            .Set(item => item.Status, CardEffectStatus.Resolved)
            .Set(item => item.Resolution, "triggered")
            .Set(item => item.TriggerAt, triggeredAt)
            .Set(item => item.TriggeredByTeamId, triggeringTeamId.ToString())
            .Set(item => item.ResolvedAt, triggeredAt)
            .Set(item => item.ModifiedAt, triggeredAt)
            .Inc(item => item.Version, 1)
            .Set(item => item.RemainingTriggers, 0);

        return await effectCollection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<CardEffectDocument>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    public async Task<int> ApplyDueRestocksAsync(
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var documents = await collection.Find(_ => true).ToListAsync(cancellationToken);
        var applied = 0;
        foreach (var document in documents)
        {
            var due = document.RestockSchedules
                .Where(item => item.Status == "pending" && item.ScheduledAt <= now)
                .ToArray();
            if (due.Length == 0) continue;

            foreach (var schedule in due)
            {
                foreach (var (cardId, quantity) in schedule.Quantities)
                {
                    var inventory = document.Inventory.FirstOrDefault(item => item.CardId == cardId);
                    if (inventory is not null) inventory.RemainingStock += quantity;
                }

                schedule.Status = "executed";
                schedule.ExecutedAt = now;
                applied++;
            }

            await ReplaceAsync(document, cancellationToken);
        }

        return applied;
    }

    private static BsonDocument ToBsonDocument(IReadOnlyDictionary<string, string> values) =>
        new(values.Select(item => new BsonElement(item.Key, item.Value)));
}
