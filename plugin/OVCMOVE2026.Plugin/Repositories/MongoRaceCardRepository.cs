using MongoDB.Driver;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE2026.Plugin.Repositories;

public sealed class MongoRaceCardRepository(
    IMongoCollection<RaceCardDocument> collection) : IRaceCardRepository
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
                    .Select(card => new CardInventoryState { CardId = card.CardId })
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

        var addedCard = false;
        foreach (var card in CardCatalog.All)
        {
            if (document.Inventory.Any(item => item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase)))
                continue;

            document.Inventory.Add(new CardInventoryState { CardId = card.CardId });
            addedCard = true;
        }

        if (addedCard)
        {
            document.ModifiedAt = DateTime.UtcNow;
            await ReplaceAsync(document, cancellationToken);
        }

        return document;
    }

    public async Task ReplaceAsync(
        RaceCardDocument document,
        CancellationToken cancellationToken = default)
    {
        document.ModifiedAt = DateTime.UtcNow;
        var result = await collection.ReplaceOneAsync(
            item => item.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        if (!result.IsAcknowledged)
            throw new InvalidOperationException("Không thể lưu dữ liệu card vào MongoDB.");
    }

    public async Task<TrapState?> TryClaimTrapAsync(
        Guid raceId,
        Guid boothId,
        Guid triggeringTeamId,
        DateTime triggeredAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<RaceCardDocument>.Filter.And(
            Builders<RaceCardDocument>.Filter.Eq(item => item.Id, raceId.ToString()),
            Builders<RaceCardDocument>.Filter.ElemMatch(
                item => item.Traps,
                trap => trap.BoothId == boothId.ToString() && trap.TriggeredAt == null));
        var update = Builders<RaceCardDocument>.Update
            .Set("traps.$.triggeredAt", triggeredAt)
            .Set("traps.$.triggeredByTeamId", triggeringTeamId.ToString());

        var document = await collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<RaceCardDocument>
            {
                ReturnDocument = ReturnDocument.Before
            },
            cancellationToken);

        return document?.Traps.LastOrDefault(trap =>
            trap.BoothId == boothId.ToString() && trap.TriggeredAt is null);
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
}
