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
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<RaceCardDocument>(
                Builders<RaceCardDocument>.IndexKeys.Ascending(item => item.RaceId),
                new CreateIndexOptions { Unique = true, Name = "ux_race_cards_race_id" }),
            cancellationToken: cancellationToken);

        await effectCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<CardEffectDocument>(
                Builders<CardEffectDocument>.IndexKeys
                    .Ascending(item => item.RaceId)
                    .Ascending(item => item.TargetBoothId),
                new CreateIndexOptions<CardEffectDocument>
                {
                    Unique = true,
                    Name = "ux_active_trap_per_booth",
                    PartialFilterExpression = new BsonDocument
                    {
                        ["cardId"] = CardIds.Trap,
                        ["status"] = CardEffectStatus.Active
                    }
                }),
            cancellationToken: cancellationToken);

        await effectCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<CardEffectDocument>(
                Builders<CardEffectDocument>.IndexKeys
                    .Ascending(item => item.RaceId)
                    .Ascending(item => item.TriggerEventCode)
                    .Ascending(item => item.TargetTeamId)
                    .Ascending(item => item.Status)
                    .Ascending(item => item.StartAt),
                new CreateIndexOptions { Name = "ix_effect_trigger_target" }),
            cancellationToken: cancellationToken);
    }

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
                        Price = ToPrice(card),
                        CardConfig = card.DefaultConfig.DeepClone().AsBsonDocument
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

        var missingDefinitions = CardCatalog.All
            .Where(definition => document.Inventory.All(item =>
                !item.CardId.Equals(definition.CardId, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var changed = missingDefinitions.Length > 0;
        if (missingDefinitions.Length > 0)
        {
            document.Inventory.AddRange(missingDefinitions.Select(definition =>
                new CardInventoryState
                {
                    CardId = definition.CardId,
                    RemainingStock = 0,
                    Price = ToPrice(definition),
                    CardConfig = definition.DefaultConfig.DeepClone().AsBsonDocument
                }));
        }

        foreach (var inventory in document.Inventory)
        {
            if (inventory.Price >= 0) continue;
            var definition = CardCatalog.TryGet(inventory.CardId);
            if (definition is null) continue;
            inventory.Price = ToPrice(definition);
            changed = true;
        }

        if (changed)
            await ReplaceAsync(document, cancellationToken);

        return document;
    }

    public async Task ReplaceAsync(
        RaceCardDocument document,
        CancellationToken cancellationToken = default)
    {
        var expectedVersion = document.Version;
        document.ModifiedAt = DateTime.UtcNow;
        document.Version = expectedVersion + 1;
        var versionFilter = MatchRaceVersion(expectedVersion);
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
            var versionFilter = MatchRaceVersion(expectedVersion);
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
        string resolvedByEventCode,
        string resolvedByEventId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<CardEffectDocument>.Filter.And(
            Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.CardId, CardIds.Trap),
            Builders<CardEffectDocument>.Filter.Eq(item => item.TargetBoothId, boothId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.TriggerEventCode, resolvedByEventCode),
            Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
            Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, null),
            Builders<CardEffectDocument>.Filter.Ne(item => item.OwnerTeamId, triggeringTeamId.ToString()),
            Builders<CardEffectDocument>.Filter.Or(
                Builders<CardEffectDocument>.Filter.Eq(item => item.LimitEndAt, null),
                Builders<CardEffectDocument>.Filter.Gt(item => item.LimitEndAt, triggeredAt)));
        var update = Builders<CardEffectDocument>.Update
            .Set(item => item.ClaimedAt, triggeredAt)
            .Set(item => item.ClaimedByEventId, resolvedByEventId)
            .Set(item => item.TriggeredByTeamId, triggeringTeamId.ToString())
            .Set(item => item.ModifiedAt, triggeredAt)
            .Inc(item => item.Version, 1);

        return await effectCollection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<CardEffectDocument>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    public Task<bool> HasPendingReviveAsync(
        Guid raceId,
        Guid teamId,
        Guid boothId,
        CancellationToken cancellationToken = default) =>
        effectCollection.Find(effect =>
                effect.RaceId == raceId.ToString() &&
                effect.CardId == CardIds.Revive &&
                effect.OwnerTeamId == teamId.ToString() &&
                effect.TargetBoothId == boothId.ToString() &&
                effect.Status == CardEffectStatus.Active)
            .AnyAsync(cancellationToken);

    public async Task<CardEffectDocument?> ResolveReviveAsync(
        Guid raceId,
        string effectId,
        Guid organizerId,
        string resolution,
        DateTime confirmedAt,
        CancellationToken cancellationToken = default)
    {
        if (resolution is not (
                CardEffectResolutionCodes.OperatorConfirmed or
                CardEffectResolutionCodes.OperatorRejected))
            throw new ArgumentOutOfRangeException(nameof(resolution));
        if (!ObjectId.TryParse(effectId, out var objectId)) return null;

        using var session = await collection.Database.Client.StartSessionAsync(
            cancellationToken: cancellationToken);
        session.StartTransaction();
        try
        {
            var effect = await effectCollection.Find(session, item =>
                    item.Id == objectId.ToString() &&
                    item.RaceId == raceId.ToString() &&
                    item.CardId == CardIds.Revive &&
                    item.Status == CardEffectStatus.Active)
                .FirstOrDefaultAsync(cancellationToken);
            if (effect is null)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return null;
            }

            var document = await collection.Find(session, item => item.Id == raceId.ToString())
                .FirstOrDefaultAsync(cancellationToken);
            var card = document?.Teams
                .SelectMany(team => team.Cards)
                .FirstOrDefault(item => item.CardInfo.CardInstanceId == effect.CardInstanceId);
            var use = card?.CardUses.FirstOrDefault(item => item.Id == effect.CardUseId);
            if (document is null || card is null || use is null ||
                use.Status != CardUseStatus.Pending || card.CardInfo.CardUseCountRemain <= 0)
            {
                await session.AbortTransactionAsync(cancellationToken);
                return null;
            }

            var expectedDocumentVersion = document.Version;
            card.CardInfo.CardUseCountRemain--;
            card.Status = card.CardInfo.CardUseCountRemain == 0 ? CardStatus.Used : CardStatus.Received;
            use.Status = CardUseStatus.Resolved;
            use.EndAt = confirmedAt;
            use.CardUseCountAfter = card.CardInfo.CardUseCountRemain;
            use.Result = new BsonDocument
            {
                ["decision"] = resolution,
                ["resolvedBy"] = organizerId.ToString(),
                ["resolvedAt"] = confirmedAt
            };
            document.ModifiedAt = confirmedAt;
            document.Version++;

            var raceVersionFilter = MatchRaceVersion(expectedDocumentVersion);
            var raceFilter = Builders<RaceCardDocument>.Filter.And(
                Builders<RaceCardDocument>.Filter.Eq(item => item.Id, document.Id),
                raceVersionFilter);
            var raceResult = await collection.ReplaceOneAsync(
                session, raceFilter, document, cancellationToken: cancellationToken);
            if (raceResult.MatchedCount != 1)
                throw new ApplicationConflictException(
                    "Dữ liệu card vừa thay đổi. Vui lòng tải lại và xác nhận lại Revive.");

            var effectFilter = Builders<CardEffectDocument>.Filter.And(
                Builders<CardEffectDocument>.Filter.Eq(item => item.Id, effect.Id),
                Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
                MatchEffectVersion(effect.Version));
            var effectUpdate = Builders<CardEffectDocument>.Update
                .Set(item => item.Status, CardEffectStatus.Resolved)
                .Set(item => item.Resolution, resolution)
                .Set(item => item.ResolvedAt, confirmedAt)
                .Set(item => item.ResolvedByEventCode, CardEffectEventCodes.ReviveOperatorConfirmation)
                .Set(item => item.ResolvedByEventId, $"revive-confirm:{effect.Id}")
                .Set(item => item.ModifiedAt, confirmedAt)
                .Set(item => item.ModifiedBy, organizerId.ToString())
                .Set(item => item.RemainingTriggers, 0)
                .Inc(item => item.Version, 1);
            var effectResult = await effectCollection.UpdateOneAsync(
                session, effectFilter, effectUpdate, cancellationToken: cancellationToken);
            if (effectResult.MatchedCount != 1)
                throw new ApplicationConflictException("Yêu cầu Revive đã được xử lý trước đó.");

            await session.CommitTransactionAsync(cancellationToken);
            effect.Status = CardEffectStatus.Resolved;
            effect.Resolution = resolution;
            effect.ResolvedAt = confirmedAt;
            return effect;
        }
        catch
        {
            if (session.IsInTransaction)
                await session.AbortTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<CardEffectDocument?> GetEffectAsync(
        Guid raceId,
        string effectId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(effectId, out var objectId)) return null;
        return await effectCollection.Find(item =>
                item.Id == objectId.ToString() && item.RaceId == raceId.ToString())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CardEffectDocument>> GetActiveBoothResultEffectsAsync(
        Guid raceId,
        Guid teamId,
        DateTime occurredAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<CardEffectDocument>.Filter.And(
            Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(
                item => item.TriggerEventCode,
                CardEffectEventCodes.BoothResultFinalized),
            Builders<CardEffectDocument>.Filter.Eq(item => item.TargetTeamId, teamId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
            Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, null),
            Builders<CardEffectDocument>.Filter.Or(
                Builders<CardEffectDocument>.Filter.Eq(item => item.LimitEndAt, null),
                Builders<CardEffectDocument>.Filter.Gt(item => item.LimitEndAt, occurredAt)));

        return await effectCollection.Find(filter)
            .SortBy(item => item.StartAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CardEffectDocument>> GetActiveEffectsByCardAsync(
        Guid raceId,
        string cardId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<CardEffectDocument>.Filter.And(
            Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.CardId, cardId),
            Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
            Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, null));
        return await effectCollection.Find(filter)
            .SortBy(item => item.StartAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task ClaimEffectsAsync(
        Guid raceId,
        IReadOnlyCollection<string> effectIds,
        string eventId,
        DateTime claimedAt,
        CancellationToken cancellationToken = default)
    {
        if (effectIds.Count == 0) return;
        if (effectIds.Count != effectIds.Distinct(StringComparer.Ordinal).Count())
            throw new ArgumentException("Effect IDs must be distinct.", nameof(effectIds));

        using var session = await collection.Database.Client.StartSessionAsync(
            cancellationToken: cancellationToken);
        session.StartTransaction();
        try
        {
            foreach (var effectId in effectIds)
            {
                var filter = Builders<CardEffectDocument>.Filter.And(
                    Builders<CardEffectDocument>.Filter.Eq(item => item.Id, effectId),
                    Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
                    Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
                    Builders<CardEffectDocument>.Filter.Or(
                        Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, null),
                        Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, eventId)));
                var update = Builders<CardEffectDocument>.Update
                    .Set(item => item.ClaimedAt, claimedAt)
                    .Set(item => item.ClaimedByEventId, eventId)
                    .Set(item => item.ModifiedAt, claimedAt)
                    .Inc(item => item.Version, 1);
                var result = await effectCollection.UpdateOneAsync(
                    session, filter, update, cancellationToken: cancellationToken);
                if (result.MatchedCount != 1)
                    throw new ApplicationConflictException(
                        "Effect đã được claim bởi một sự kiện khác.");
            }

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            if (session.IsInTransaction)
                await session.AbortTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<CardEffectDocument>> GetClaimedEffectsAsync(
        Guid raceId,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<CardEffectDocument>.Filter.And(
            Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
            Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, eventId));
        return await effectCollection.Find(filter)
            .SortBy(item => item.ClaimedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task CompleteClaimedEffectsAsync(
        Guid raceId,
        string eventCode,
        string eventId,
        Guid triggeredByTeamId,
        DateTime resolvedAt,
        IReadOnlyCollection<CardEffectResolution> resolutions,
        CancellationToken cancellationToken = default)
    {
        if (resolutions.Count == 0) return;

        var resolutionIds = resolutions.Select(item => item.EffectId).ToArray();
        var alreadyCompletedFilter = Builders<CardEffectDocument>.Filter.And(
            Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
            Builders<CardEffectDocument>.Filter.In(item => item.Id, resolutionIds),
            Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Resolved),
            Builders<CardEffectDocument>.Filter.Eq(item => item.ResolvedByEventId, eventId));
        var alreadyCompleted = await effectCollection.CountDocumentsAsync(
            alreadyCompletedFilter,
            cancellationToken: cancellationToken);
        if (alreadyCompleted == resolutions.Count) return;

        using var session = await collection.Database.Client.StartSessionAsync(
            cancellationToken: cancellationToken);
        session.StartTransaction();
        try
        {
            var raceKey = raceId.ToString();
            var document = await collection.Find(session, item => item.Id == raceKey)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ApplicationNotFoundException("Không tìm thấy dữ liệu card của race.");
            var expectedVersion = document.Version;

            foreach (var resolution in resolutions)
            {
                var card = document.Teams
                    .SelectMany(team => team.Cards)
                    .FirstOrDefault(item => item.CardUses.Any(use =>
                        use.EffectId == resolution.EffectId));
                var use = card?.CardUses.FirstOrDefault(item =>
                    item.EffectId == resolution.EffectId);
                if (card is null || use is null || use.Status != CardUseStatus.Active)
                    throw new ApplicationConflictException(
                        "Lượt dùng card đã được xử lý bởi yêu cầu khác.");

                use.Status = CardUseStatus.Resolved;
                use.EndAt = resolvedAt;
                use.Result = resolution.Result.DeepClone().AsBsonDocument;
                if (resolution.NextTimeAvailable.HasValue)
                    card.NextTimeAvailable = resolution.NextTimeAvailable;

                var effectFilter = Builders<CardEffectDocument>.Filter.And(
                    Builders<CardEffectDocument>.Filter.Eq(item => item.Id, resolution.EffectId),
                    Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceKey),
                    Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
                    Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, eventId));
                var effectUpdate = Builders<CardEffectDocument>.Update
                    .Set(item => item.Status, CardEffectStatus.Resolved)
                    .Set(item => item.Resolution, resolution.Resolution)
                    .Set(item => item.TriggerAt, resolvedAt)
                    .Set(item => item.ResolvedByEventCode, eventCode)
                    .Set(item => item.ResolvedByEventId, eventId)
                    .Set(item => item.TriggeredByTeamId, triggeredByTeamId.ToString())
                    .Set(item => item.ResolvedAt, resolvedAt)
                    .Set(item => item.ModifiedAt, resolvedAt)
                    .Unset(item => item.ClaimedAt)
                    .Unset(item => item.ClaimedByEventId)
                    .Set(item => item.RemainingTriggers, 0)
                    .Inc(item => item.Version, 1);
                var effectResult = await effectCollection.UpdateOneAsync(
                    session,
                    effectFilter,
                    effectUpdate,
                    cancellationToken: cancellationToken);
                if (effectResult.MatchedCount != 1)
                    throw new ApplicationConflictException(
                        "Effect đã được xử lý bởi yêu cầu khác.");
            }

            document.ModifiedAt = resolvedAt;
            document.Version++;
            var raceFilter = Builders<RaceCardDocument>.Filter.And(
                Builders<RaceCardDocument>.Filter.Eq(item => item.Id, document.Id),
                MatchRaceVersion(expectedVersion));
            var raceResult = await collection.ReplaceOneAsync(
                session,
                raceFilter,
                document,
                cancellationToken: cancellationToken);
            if (raceResult.MatchedCount != 1)
                throw new ApplicationConflictException(
                    "Dữ liệu card vừa được cập nhật. Vui lòng thử lại.");

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            if (session.IsInTransaction)
                await session.AbortTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task ReleaseClaimedEffectsAsync(
        Guid raceId,
        string eventId,
        DateTime releasedAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<CardEffectDocument>.Filter.And(
            Builders<CardEffectDocument>.Filter.Eq(item => item.RaceId, raceId.ToString()),
            Builders<CardEffectDocument>.Filter.Eq(item => item.Status, CardEffectStatus.Active),
            Builders<CardEffectDocument>.Filter.Eq(item => item.ClaimedByEventId, eventId));
        var update = Builders<CardEffectDocument>.Update
            .Unset(item => item.ClaimedAt)
            .Unset(item => item.ClaimedByEventId)
            .Unset(item => item.TriggeredByTeamId)
            .Set(item => item.ModifiedAt, releasedAt)
            .Inc(item => item.Version, 1);
        await effectCollection.UpdateManyAsync(
            filter, update, cancellationToken: cancellationToken);
    }

    private static FilterDefinition<RaceCardDocument> MatchRaceVersion(long expectedVersion) =>
        expectedVersion == 0
            ? Builders<RaceCardDocument>.Filter.Or(
                Builders<RaceCardDocument>.Filter.Eq(item => item.Version, 0),
                Builders<RaceCardDocument>.Filter.Exists("version", false))
            : Builders<RaceCardDocument>.Filter.Eq(item => item.Version, expectedVersion);

    private static FilterDefinition<CardEffectDocument> MatchEffectVersion(long expectedVersion) =>
        expectedVersion == 0
            ? Builders<CardEffectDocument>.Filter.Or(
                Builders<CardEffectDocument>.Filter.Eq(item => item.Version, 0),
                Builders<CardEffectDocument>.Filter.Exists("version", false))
            : Builders<CardEffectDocument>.Filter.Eq(item => item.Version, expectedVersion);

    private static int ToPrice(CardDefinition definition) =>
        decimal.ToInt32(definition.Price);

}
