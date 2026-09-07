using System.Globalization;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Abstractions.Plugins;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public sealed class RaceCardService(IRaceCardRepository repository) : IRaceCardService
{
    public async Task<CardStoreOverviewResponse> GetAdminOverviewAsync(
        Guid raceId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        return new CardStoreOverviewResponse(
            document.StoreOpen,
            CardCatalog.All.Select(card => ToInventoryResponse(card, document)).ToArray());
    }

    public async Task<IReadOnlyCollection<CardTeamResponse>> GetCardTeamsAsync(
        Guid raceId,
        string cardId,
        CancellationToken cancellationToken = default)
    {
        var definition = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        return document.Teams
            .SelectMany(team => team.Cards
                .Where(card => SameCard(card, definition.CardId))
                .Select(card => ToTeamResponse(team, card, definition)))
            .OrderByDescending(item => item.ReceivedAt)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TeamCardResponse>> GetTeamCardsAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var team = FindTeam(document, teamId);
        if (team is null) return [];

        return team.Cards
            .Where(card => card.Status != CardStatus.Deleted)
            .OrderByDescending(card => card.ReceivedAt)
            .Select(card => ToTeamCardResponse(card, document))
            .ToArray();
    }

    public async Task<TeamCardResponse> GetTeamCardAsync(
        Guid raceId,
        Guid teamId,
        Guid cardInstanceId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var team = FindTeam(document, teamId);
        var card = team?.Cards.LastOrDefault(item =>
            SameCardInstance(item, cardInstanceId) && item.Status != CardStatus.Deleted);
        return card is null
            ? throw new ApplicationNotFoundException("Không tìm thấy card trong kho của đội.")
            : ToTeamCardResponse(card, document);
    }

    public async Task SetStoreOpenAsync(
        Guid raceId,
        bool isOpen,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        document.StoreOpen = isOpen;
        await repository.ReplaceAsync(document, cancellationToken);
    }

    public async Task RestockAsync(
        Guid raceId,
        IReadOnlyDictionary<string, int> quantities,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        ApplyQuantities(document, quantities);
        await repository.ReplaceAsync(document, cancellationToken);
    }

    public async Task ScheduleRestockAsync(
        Guid raceId,
        DateTime scheduledAt,
        IReadOnlyDictionary<string, int> quantities,
        CancellationToken cancellationToken = default)
    {
        if (scheduledAt.ToUniversalTime() <= DateTime.UtcNow)
            throw new ApplicationValidationException("Thời gian hẹn nhập phải ở tương lai.");

        ValidateQuantities(quantities);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        document.RestockSchedules.Add(new RestockScheduleState
        {
            ScheduledAt = scheduledAt.ToUniversalTime(),
            Quantities = quantities.ToDictionary(item => item.Key.ToUpperInvariant(), item => item.Value),
            CreatedAt = DateTime.UtcNow
        });
        await repository.ReplaceAsync(document, cancellationToken);
    }

    public async Task UpdateConfigAsync(
        Guid raceId,
        string cardId,
        IReadOnlyDictionary<string, JsonElement> config,
        CancellationToken cancellationToken = default)
    {
        var definition = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var inventory = FindInventory(document, definition.CardId);
        var supportedKeys = definition.DefaultConfig.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unsupportedKey = config.Keys.FirstOrDefault(key => !supportedKeys.Contains(key));
        if (unsupportedKey is not null)
            throw new ApplicationValidationException($"Không hỗ trợ cấu hình '{unsupportedKey}' cho card này.");

        foreach (var key in definition.DefaultConfig.Keys)
        {
            if (!config.TryGetValue(key, out var value)) continue;
            if (key == "penaltyPoints" &&
                (value.ValueKind != JsonValueKind.Number ||
                 !value.TryGetInt32(out var points) ||
                 points <= 0))
            {
                throw new ApplicationValidationException("Số điểm bị trừ phải là số nguyên dương.");
            }
            inventory.CardConfig[key] = ToBsonValue(value);
        }

        await repository.ReplaceAsync(document, cancellationToken);
    }

    public async Task<CardTeamResponse> AssignAsync(
        Guid raceId,
        string cardId,
        Guid teamId,
        string teamName,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (teamId == Guid.Empty || string.IsNullOrWhiteSpace(teamName))
            throw new ApplicationValidationException("TeamId và tên team là bắt buộc.");

        var definition = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);

        var inventory = FindInventory(document, definition.CardId);
        if (inventory.RemainingStock <= 0)
            throw new ApplicationConflictException("Card đã hết trong kho.");

        var team = FindTeam(document, teamId);
        if (team is null)
        {
            team = new RaceCardTeamState
            {
                TeamId = teamId.ToString(),
                TeamName = teamName.Trim()
            };
            document.Teams.Add(team);
        }
        else
        {
            team.TeamName = teamName.Trim();
        }

        var card = new TeamCardState
        {
            CardInfo = new TeamCardInfo
            {
                CardInstanceId = Guid.NewGuid().ToString(),
                CardId = definition.CardId,
                CardUseCountRemain = GetUseCount(inventory)
            },
            ReceivedAt = DateTime.UtcNow,
            ReceiveReason = reason?.Trim() ?? string.Empty
        };
        team.Cards.Add(card);
        inventory.RemainingStock--;

        await repository.ReplaceAsync(document, cancellationToken);
        return ToTeamResponse(team, card, definition);
    }

    public async Task DeleteAssignmentAsync(
        Guid raceId,
        Guid cardInstanceId,
        Guid teamId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ApplicationValidationException("Lý do xóa là bắt buộc.");

        var document = await GetDocumentAsync(raceId, cancellationToken);
        var team = FindTeam(document, teamId);
        var card = team?.Cards.LastOrDefault(item =>
            SameCardInstance(item, cardInstanceId) &&
            item.Status == CardStatus.Received &&
            item.CardUses.Count == 0);
        if (card is null)
            throw new ApplicationConflictException("Chỉ được xóa card chưa sử dụng.");

        var definition = CardCatalog.Get(card.CardInfo.CardId);

        card.Status = CardStatus.Deleted;
        card.DisabledAt = DateTime.UtcNow;
        card.DisabledReason = reason.Trim();
        FindInventory(document, definition.CardId).RemainingStock++;
        await repository.ReplaceAsync(document, cancellationToken);
    }

    public async Task<CardUseResponse> UseAsync(
        Guid raceId,
        Guid teamId,
        Guid cardInstanceId,
        IReadOnlyDictionary<string, string> inputs,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);

        var team = FindTeam(document, teamId);
        var card = team?.Cards.LastOrDefault(item =>
            SameCardInstance(item, cardInstanceId) &&
            item.Status == CardStatus.Received &&
            item.CardInfo.CardUseCountRemain > 0);
        if (card is null)
            throw new ApplicationConflictException("Card không còn sẵn sàng để sử dụng.");

        var definition = CardCatalog.Get(card.CardInfo.CardId);

        CardEffectDocument? effect = null;
        var now = DateTime.UtcNow;
        var cardUseId = Guid.NewGuid().ToString();
        if (definition.CardId == CardIds.Trap)
        {
            if (!inputs.TryGetValue("boothId", out var boothId) || !Guid.TryParse(boothId, out _))
                throw new ApplicationValidationException("Trap cần input boothId hợp lệ.");
            if (await repository.HasActiveTrapAsync(raceId, Guid.Parse(boothId), cancellationToken))
                throw new ApplicationConflictException("Trạm này đã có bẫy đang hoạt động.");

            effect = new CardEffectDocument
            {
                RaceId = raceId.ToString(),
                CardId = definition.CardId,
                CardInstanceId = card.CardInfo.CardInstanceId,
                CardUseId = cardUseId,
                OwnerTeamId = teamId.ToString(),
                TargetBoothId = boothId,
                TriggerEventCode = PluginEventNames.BoothEntryRequested,
                StartAt = now,
                CreatedAt = now,
                CreatedBy = teamId.ToString(),
                Data = new BsonDocument("penaltyPoints", GetPenalty(document, definition))
            };
        }

        var countBefore = card.CardInfo.CardUseCountRemain;
        card.CardInfo.CardUseCountRemain--;
        card.CardUses.Add(new CardUseState
        {
            Id = cardUseId,
            EffectId = effect?.Id,
            Status = CardUseStatus.Succeeded,
            Target = inputs.ToDictionary(item => item.Key, item => item.Value),
            UseAt = now,
            EndAt = now,
            CardUseCountBefore = countBefore,
            CardUseCountAfter = card.CardInfo.CardUseCountRemain
        });
        if (card.CardInfo.CardUseCountRemain == 0)
            card.Status = CardStatus.Used;

        if (effect is null)
            await repository.ReplaceAsync(document, cancellationToken);
        else
            await repository.ReplaceWithEffectAsync(document, effect, cancellationToken);
        var cardUse = card.CardUses[^1];
        return new CardUseResponse(
            cardUse.Id,
            card.CardInfo.CardInstanceId,
            definition.CardId,
            definition.CardName,
            cardUse.Status,
            cardUse.UseAt,
            "Đã sử dụng card.");
    }

    public Task<CardEffectDocument?> TriggerTrapAsync(
        Guid raceId,
        Guid boothId,
        Guid teamId,
        DateTime occurredAt,
        string eventCode,
        string eventId,
        CancellationToken cancellationToken = default) =>
        repository.TryClaimTrapAsync(
            raceId,
            boothId,
            teamId,
            occurredAt,
            eventCode,
            eventId,
            cancellationToken);

    private async Task<RaceCardDocument> GetDocumentAsync(Guid raceId, CancellationToken cancellationToken)
    {
        if (raceId == Guid.Empty)
            throw new ApplicationNotFoundException("Không tìm thấy race.");
        try
        {
            return await repository.GetOrCreateAsync(raceId, cancellationToken);
        }
        catch (MongoAuthenticationException exception)
        {
            throw new ApplicationServiceUnavailableException("Không thể xác thực với MongoDB.", exception);
        }
        catch (MongoConnectionException exception)
        {
            throw new ApplicationServiceUnavailableException("Không thể kết nối MongoDB để tải dữ liệu card.", exception);
        }
    }

    private static CardInventoryResponse ToInventoryResponse(CardDefinition definition, RaceCardDocument document)
    {
        var inventory = FindInventory(document, definition.CardId);
        var config = definition.DefaultConfig.ToDictionary(item => item.Key, item => item.Value);
        foreach (var element in inventory.CardConfig)
            config[element.Name] = ToConfigString(element.Value);
        return new CardInventoryResponse(
            definition.CardId,
            definition.CardName,
            definition.Description,
            definition.Price,
            inventory.RemainingStock,
            definition.Usage,
            definition.Inputs,
            config);
    }

    private static CardTeamResponse ToTeamResponse(
        RaceCardTeamState team,
        TeamCardState card,
        CardDefinition definition) => new(
        team.TeamId,
        team.TeamName,
        card.CardInfo.CardInstanceId,
        definition.CardId,
        definition.CardName,
        card.CardInfo.CardUseCountRemain,
        card.ReceivedAt,
        card.ReceiveReason,
        card.Status,
        card.Status == CardStatus.Received,
        card.DisabledAt,
        card.DisabledReason,
        card.CardUses);

    private static TeamCardResponse ToTeamCardResponse(TeamCardState card, RaceCardDocument document)
    {
        var definition = CardCatalog.Get(card.CardInfo.CardId);
        var inventory = FindInventory(document, definition.CardId);
        var config = definition.DefaultConfig.ToDictionary(item => item.Key, item => item.Value);
        foreach (var element in inventory.CardConfig)
            config[element.Name] = ToConfigString(element.Value);
        return new TeamCardResponse(
            card.CardInfo.CardInstanceId,
            definition.CardId,
            definition.CardName,
            definition.Description,
            definition.Usage,
            definition.Inputs,
            config,
            card.CardInfo.CardUseCountRemain,
            card.ReceivedAt,
            card.ReceiveReason,
            card.Status,
            card.CardUses);
    }

    private static RaceCardTeamState? FindTeam(RaceCardDocument document, Guid teamId) =>
        document.Teams.FirstOrDefault(team => team.TeamId == teamId.ToString());

    private static CardInventoryState FindInventory(RaceCardDocument document, string cardId) =>
        document.Inventory.Single(item => item.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase));

    private static bool SameCard(TeamCardState card, string cardId) =>
        card.CardInfo.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase);

    private static bool SameCardInstance(TeamCardState card, Guid cardInstanceId) =>
        Guid.TryParse(card.CardInfo.CardInstanceId, out var currentId) && currentId == cardInstanceId;

    private static int GetUseCount(CardInventoryState inventory) =>
        inventory.CardConfig.TryGetValue("card_use_count_max", out var value) &&
        int.TryParse(ToConfigString(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) && count > 0
            ? count
            : 1;

    private static void ApplyQuantities(RaceCardDocument document, IReadOnlyDictionary<string, int> quantities)
    {
        ValidateQuantities(quantities);
        foreach (var (cardId, quantity) in quantities)
            FindInventory(document, CardCatalog.Get(cardId).CardId).RemainingStock += quantity;
    }

    private static void ValidateQuantities(IReadOnlyDictionary<string, int> quantities)
    {
        foreach (var (cardId, quantity) in quantities)
        {
            _ = CardCatalog.Get(cardId);
            if (quantity < 0)
                throw new ApplicationValidationException("Số lượng nhập không được âm.");
        }
    }

    private static int GetPenalty(RaceCardDocument document, CardDefinition definition)
    {
        var inventory = FindInventory(document, definition.CardId);
        var value = inventory.CardConfig.TryGetValue("penaltyPoints", out var configured)
            ? ToConfigString(configured)
            : definition.DefaultConfig.GetValueOrDefault("penaltyPoints");
        return int.TryParse(value, out var points) && points > 0 ? points : 10;
    }

    private static string ToConfigString(BsonValue value) =>
        value.IsString ? value.AsString : value.ToJson();

    private static BsonValue ToBsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => new BsonDocument(value.EnumerateObject()
            .Select(property => new BsonElement(property.Name, ToBsonValue(property.Value)))),
        JsonValueKind.Array => new BsonArray(value.EnumerateArray().Select(ToBsonValue)),
        JsonValueKind.String => new BsonString(value.GetString() ?? string.Empty),
        JsonValueKind.Number when value.TryGetInt32(out var integer) => new BsonInt32(integer),
        JsonValueKind.Number when value.TryGetInt64(out var longInteger) => new BsonInt64(longInteger),
        JsonValueKind.Number when value.TryGetDecimal(out var decimalNumber) => new BsonDecimal128(decimalNumber),
        JsonValueKind.Number => new BsonDouble(value.GetDouble()),
        JsonValueKind.True => BsonBoolean.True,
        JsonValueKind.False => BsonBoolean.False,
        JsonValueKind.Null => BsonNull.Value,
        _ => throw new ApplicationValidationException("Giá trị cấu hình card không hợp lệ.")
    };
}
