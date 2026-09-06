using System.Globalization;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Abstractions.Plugins;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public sealed class RaceCardService(
    IRaceCardRepository repository,
    CardUseHandlerResolver handlerResolver,
    ISender sender,
    IBoothRepository boothRepository,
    IBoothOrganizerRepository boothOrganizerRepository,
    IRaceRepository raceRepository,
    ILogger<RaceCardService> logger) : IRaceCardService
{
    public async Task<CardStoreOverviewResponse> GetAdminOverviewAsync(
        Guid raceId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        return new CardStoreOverviewResponse(
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

        var activeBooth = await boothRepository.GetActiveByTeamAndRaceAsync(
            teamId,
            raceId,
            cancellationToken);
        var now = DateTime.UtcNow;

        return team.Cards
            .Where(card => card.Status != CardStatus.Deleted)
            .OrderByDescending(card => card.ReceivedAt)
            .Select(card => ToTeamCardResponse(card, document, activeBooth, now))
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
        if (card is null)
            throw new ApplicationNotFoundException("Không tìm thấy card trong kho của đội.");
        var activeBooth = await boothRepository.GetActiveByTeamAndRaceAsync(
            teamId,
            raceId,
            cancellationToken);
        return ToTeamCardResponse(card, document, activeBooth, DateTime.UtcNow);
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

    public async Task UpdateConfigAsync(
        Guid raceId,
        string cardId,
        IReadOnlyDictionary<string, JsonElement> config,
        CancellationToken cancellationToken = default)
    {
        var definition = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var inventory = FindInventory(document, definition.CardId);
        var supportedKeys = definition.DefaultConfig.Names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unsupportedKey = config.Keys.FirstOrDefault(key => !supportedKeys.Contains(key));
        if (unsupportedKey is not null)
            throw new ApplicationValidationException($"Không hỗ trợ cấu hình '{unsupportedKey}' cho card này.");

        var normalizedConfig = config.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        foreach (var key in definition.DefaultConfig.Names)
        {
            if (!normalizedConfig.TryGetValue(key, out var value)) continue;
            ValidateConfigValue(definition, key, value);
            if (key == "card_use_count_max" &&
                document.Teams.SelectMany(team => team.Cards).Any(card =>
                    SameCard(card, definition.CardId) && card.Status != CardStatus.Deleted) &&
                value.GetInt32() != GetUseCount(inventory, definition))
                throw new ApplicationConflictException(
                    "Không thể đổi số lượt tối đa sau khi card đã được cấp.");
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
        if (!await raceRepository.IsTeamInRaceAsync(raceId, teamId, cancellationToken))
            throw new ApplicationValidationException("Team được chọn không tham gia race này.");

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

        if (definition.CardType == CardTypes.CoreChip && team.Cards.Any(item =>
                item.Status != CardStatus.Deleted &&
                CardCatalog.TryGet(item.CardInfo.CardId)?.CardType == CardTypes.CoreChip))
            throw new ApplicationConflictException("Mỗi team chỉ được sở hữu một Core Chip.");
        if (definition.CardId == CardIds.Trap && team.Cards.Any(item =>
                SameCard(item, CardIds.Trap) && item.Status != CardStatus.Deleted))
            throw new ApplicationConflictException("Mỗi team chỉ được nhận một Trap trong race.");

        var card = new TeamCardState
        {
            CardInfo = new TeamCardInfo
            {
                CardInstanceId = Guid.NewGuid().ToString(),
                CardId = definition.CardId,
                CardUseCountRemain = GetUseCount(inventory, definition)
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
        Guid cardUseId,
        BsonDocument inputs,
        CancellationToken cancellationToken = default)
    {
        if (cardUseId == Guid.Empty)
            throw new ApplicationValidationException("cardUseId là bắt buộc.");

        var document = await GetDocumentAsync(raceId, cancellationToken);

        var previous = FindCardUse(document, cardUseId.ToString());
        if (previous is not null)
        {
            if (previous.Value.TeamId != teamId.ToString() ||
                previous.Value.CardInstanceId != cardInstanceId.ToString())
                throw new ApplicationConflictException("cardUseId đã được dùng cho thao tác khác.");
            var previousDefinition = CardCatalog.Get(previous.Value.CardId);
            return ToUseResponse(previousDefinition, previous.Value.CardInstanceId, previous.Value.Use,
                "Yêu cầu này đã được xử lý trước đó.");
        }

        var team = FindTeam(document, teamId);
        var card = team?.Cards.LastOrDefault(item =>
            SameCardInstance(item, cardInstanceId) &&
            item.Status == CardStatus.Received &&
            item.CardInfo.CardUseCountRemain > 0);
        if (card is null)
            throw new ApplicationConflictException("Card không còn sẵn sàng để sử dụng.");

        var definition = CardCatalog.Get(card.CardInfo.CardId);
        var now = DateTime.UtcNow;
        if (definition.CardId == CardIds.Overclock)
            throw new ApplicationConflictException(
                "Màn dự đoán Overclock chưa được admin mở.");
        if (card.NextTimeAvailable.HasValue && card.NextTimeAvailable.Value > now)
            throw new ApplicationConflictException(
                $"Card đang hồi; có thể dùng lại sau {card.NextTimeAvailable.Value:O}.");

        if (definition.CardId is CardIds.Engineer or CardIds.Athlete or CardIds.Swap)
        {
            var activeBooth = await boothRepository.GetActiveByTeamAndRaceAsync(
                teamId,
                raceId,
                cancellationToken);
            if (activeBooth is not null)
                throw new ApplicationConflictException(
                    "Card này chỉ được dùng khi đội đang ở giữa hai booth.");
        }

        if (definition.CardId is CardIds.Cupid or CardIds.Swap)
        {
            var targetTeamId = GetRequiredGuidInput(
                inputs,
                "targetTeamId",
                $"{definition.CardName} cần targetTeamId hợp lệ.");
            if (!await raceRepository.IsTeamInRaceAsync(raceId, targetTeamId, cancellationToken))
                throw new ApplicationValidationException(
                    "Đội được chọn không tham gia race này.");
        }

        if (definition.CardId == CardIds.Trap)
        {
            var boothId = GetRequiredGuidInput(inputs, "boothId", "Trap cần input boothId hợp lệ.");
            if (await repository.HasActiveTrapAsync(raceId, boothId, cancellationToken))
                throw new ApplicationConflictException("Booth này đã có Trap đang hoạt động.");
        }

        var inventory = FindInventory(document, definition.CardId);
        var context = new CardUseContext(
            raceId,
            teamId,
            inventory,
            card,
            cardUseId.ToString(),
            inputs.DeepClone().AsBsonDocument,
            now);
        var plan = await handlerResolver.Resolve(definition.CardId)
            .PrepareAsync(context, cancellationToken);

        var countBefore = card.CardInfo.CardUseCountRemain;
        if (plan.ConsumeNow)
            card.CardInfo.CardUseCountRemain--;
        var cardUse = new CardUseState
        {
            Id = cardUseId.ToString(),
            EffectId = plan.Effect?.Id,
            Status = plan.Status,
            Inputs = inputs.DeepClone().AsBsonDocument,
            UseAt = now,
            EndAt = plan.Status == CardUseStatus.Resolved ? now : null,
            Result = plan.Result?.DeepClone().AsBsonDocument,
            CardUseCountBefore = countBefore,
            CardUseCountAfter = card.CardInfo.CardUseCountRemain
        };
        card.CardUses.Add(cardUse);
        if (card.CardInfo.CardUseCountRemain == 0)
            card.Status = CardStatus.Used;

        if (plan.Effect is null)
            await repository.ReplaceAsync(document, cancellationToken);
        else
            await repository.ReplaceWithEffectAsync(document, plan.Effect, cancellationToken);

        var notificationsSynced = await TrySendNotificationsAsync(
            raceId, teamId, plan.Notifications, cancellationToken);
        var message = notificationsSynced
            ? plan.Message
            : $"{plan.Message} Thông báo chưa đồng bộ; vui lòng tải lại hoặc liên hệ BTC.";
        return ToUseResponse(definition, card.CardInfo.CardInstanceId, cardUse, message);
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

    public async Task ConfirmReviveAsync(
        Guid raceId,
        string effectId,
        Guid organizerId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            throw new ApplicationValidationException("effectId là bắt buộc.");
        var pendingEffect = await repository.GetEffectAsync(raceId, effectId, cancellationToken);
        if (pendingEffect is null || pendingEffect.CardId != CardIds.Revive ||
            pendingEffect.Status != CardEffectStatus.Active ||
            !Guid.TryParse(pendingEffect.TargetBoothId, out var boothId) ||
            !Guid.TryParse(pendingEffect.OwnerTeamId, out var ownerTeamId))
            throw new ApplicationConflictException("Yêu cầu Revive không còn chờ xác nhận.");

        var booth = await boothRepository.GetByIdAsync(boothId, cancellationToken);
        if (booth is null || booth.RaceId != raceId || booth.TeamId != ownerTeamId ||
            booth.Status != BoothConstants.BoothStatus.Occupied)
            throw new ApplicationConflictException("Booth đã kết thúc nên không thể xác nhận Revive.");
        if (!isAdmin && !await boothOrganizerRepository.IsAssignedAsync(
                organizerId, boothId, cancellationToken))
            throw new ApplicationForbiddenException("Bạn không quản lý booth này.");

        var effect = await repository.ConfirmReviveAsync(
            raceId, effectId, organizerId, DateTime.UtcNow, cancellationToken);
        if (effect is null)
            throw new ApplicationConflictException("Yêu cầu Revive không còn chờ xác nhận.");

        if (Guid.TryParse(effect.OwnerTeamId, out var teamId))
            await TrySendNotificationsAsync(
                raceId,
                organizerId,
                [new(RaceMessageRecipientConstants.Team, teamId,
                    "Quản trạm đã xác nhận Revive. Bạn được chơi lại booth hiện tại.")],
                cancellationToken);
    }

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
        var config = ToDictionary(definition.DefaultConfig);
        foreach (var element in inventory.CardConfig)
            config[element.Name] = BsonTypeMapper.MapToDotNetValue(element.Value);
        return new CardInventoryResponse(
            definition.CardId,
            definition.CardName,
            definition.CardType,
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
        definition.CardType,
        card.CardInfo.CardUseCountRemain,
        card.ReceivedAt,
        card.ReceiveReason,
        card.Status,
        card.Status == CardStatus.Received && card.CardUses.Count == 0,
        card.DisabledAt,
        card.DisabledReason,
        card.CardUses.Select(ToCardUseHistory).ToArray());

    private static TeamCardResponse ToTeamCardResponse(
        TeamCardState card,
        RaceCardDocument document,
        OVCMOVE.Domain.Entities.Booth? activeBooth,
        DateTime now)
    {
        var definition = CardCatalog.Get(card.CardInfo.CardId);
        var inventory = FindInventory(document, definition.CardId);
        var config = ToDictionary(definition.DefaultConfig);
        foreach (var element in inventory.CardConfig)
            config[element.Name] = BsonTypeMapper.MapToDotNetValue(element.Value);
        return new TeamCardResponse(
            card.CardInfo.CardInstanceId,
            definition.CardId,
            definition.CardName,
            definition.CardType,
            definition.Description,
            definition.Usage,
            definition.Inputs,
            config,
            card.CardInfo.CardUseCountRemain,
            card.ReceivedAt,
            card.ReceiveReason,
            card.Status,
            GetAvailability(card, definition, activeBooth, now),
            card.CardUses.Select(ToCardUseHistory).ToArray());
    }

    private static CardAvailabilityResponse GetAvailability(
        TeamCardState card,
        CardDefinition definition,
        OVCMOVE.Domain.Entities.Booth? activeBooth,
        DateTime now)
    {
        if (card.Status != CardStatus.Received || card.CardInfo.CardUseCountRemain <= 0)
            return new(false, "used", "Card đã hết lượt sử dụng.", null);
        if (card.CardUses.Any(use => use.Status == CardUseStatus.Pending))
            return new(false, "pending_confirmation", "Card đang chờ xác nhận.", null);
        if (card.CardUses.Any(use => use.Status == CardUseStatus.Active))
            return new(false, "effect_active", "Effect trước vẫn đang hoạt động.", null);
        if (card.NextTimeAvailable.HasValue && card.NextTimeAvailable.Value > now)
            return new(false, "cooldown", "Card đang trong thời gian hồi.", card.NextTimeAvailable);
        if (definition.CardId == CardIds.Overclock)
            return new(false, "backend_not_ready", "Màn dự đoán Overclock chưa được mở.", null);
        if (definition.CardId == CardIds.Revive &&
            (activeBooth is null || activeBooth.Status != BoothConstants.BoothStatus.Occupied))
            return new(false, "not_in_booth", "Revive chỉ dùng khi đội đang chơi booth.", null);
        if (definition.CardId is CardIds.Engineer or CardIds.Athlete or CardIds.Swap &&
            activeBooth is not null)
            return new(false, "not_between_booths", "Card chỉ dùng giữa hai booth.", null);
        return new(true, "available", "Card có thể sử dụng.", null);
    }

    private static CardUseHistoryResponse ToCardUseHistory(CardUseState use) => new(
        use.Id,
        use.EffectId,
        use.Status,
        ToDictionary(use.Inputs),
        use.UseAt,
        use.EndAt,
        use.FailureReason,
        use.Result is null ? null : ToDictionary(use.Result));

    private static RaceCardTeamState? FindTeam(RaceCardDocument document, Guid teamId) =>
        document.Teams.FirstOrDefault(team => team.TeamId == teamId.ToString());

    private static CardInventoryState FindInventory(RaceCardDocument document, string cardId) =>
        document.Inventory.Single(item => item.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase));

    private static bool SameCard(TeamCardState card, string cardId) =>
        card.CardInfo.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase);

    private static bool SameCardInstance(TeamCardState card, Guid cardInstanceId) =>
        Guid.TryParse(card.CardInfo.CardInstanceId, out var currentId) && currentId == cardInstanceId;

    private static int GetUseCount(
        CardInventoryState inventory,
        CardDefinition definition)
    {
        var value = inventory.CardConfig.TryGetValue("card_use_count_max", out var configured)
            ? configured
            : definition.DefaultConfig.GetValue("card_use_count_max", 1);
        return int.TryParse(
                   ToConfigString(value),
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out var count) && count > 0
            ? count
            : 1;
    }

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

    private async Task<bool> TrySendNotificationsAsync(
        Guid raceId,
        Guid senderId,
        IReadOnlyCollection<CardUseNotification>? notifications,
        CancellationToken cancellationToken)
    {
        if (notifications is null || notifications.Count == 0) return true;
        var synchronized = true;
        foreach (var notification in notifications)
        {
            try
            {
                var recipient = notification.RecipientType switch
                {
                    RaceMessageRecipientConstants.Team when notification.RecipientId.HasValue =>
                        new RaceMessageRecipientModel
                        {
                            Type = RaceMessageRecipientConstants.Team,
                            Key = $"{RaceMessageRecipientConstants.TeamKeyPrefix}{notification.RecipientId.Value:D}",
                            Label = "Đội được chọn"
                        },
                    RaceMessageRecipientConstants.AllOrganizers =>
                        new RaceMessageRecipientModel
                        {
                            Type = RaceMessageRecipientConstants.AllOrganizers,
                            Key = RaceMessageRecipientConstants.AllOrganizers,
                            Label = "Ban tổ chức"
                        },
                    _ => throw new ApplicationValidationException("Loại người nhận thông báo card không hợp lệ.")
                };
                await sender.Send(new SendRaceMessageCommand
                {
                    RaceId = raceId,
                    SenderId = senderId,
                    Recipients = [recipient],
                    Body = notification.Message
                }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                synchronized = false;
                logger.LogWarning(exception, "Card notification failed for race {RaceId}.", raceId);
            }
        }
        return synchronized;
    }

    private static (string TeamId, string CardInstanceId, string CardId, CardUseState Use)? FindCardUse(
        RaceCardDocument document,
        string cardUseId)
    {
        foreach (var team in document.Teams)
        foreach (var card in team.Cards)
        {
            var use = card.CardUses.FirstOrDefault(item =>
                item.Id.Equals(cardUseId, StringComparison.OrdinalIgnoreCase));
            if (use is not null)
                return (team.TeamId, card.CardInfo.CardInstanceId, card.CardInfo.CardId, use);
        }
        return null;
    }

    private static CardUseResponse ToUseResponse(
        CardDefinition definition,
        string cardInstanceId,
        CardUseState use,
        string message) => new(
            use.Id,
            use.EffectId,
            cardInstanceId,
            definition.CardId,
            definition.CardName,
            use.Status,
            use.UseAt,
            use.EndAt,
            message);

    private static Guid GetRequiredGuidInput(BsonDocument inputs, string key, string message)
    {
        if (!inputs.TryGetValue(key, out var value) || !value.IsString ||
            !Guid.TryParse(value.AsString, out var id) || id == Guid.Empty)
            throw new ApplicationValidationException(message);
        return id;
    }

    private static Dictionary<string, object?> ToDictionary(BsonDocument document) =>
        document.Elements.ToDictionary(
            item => item.Name,
            item => (object?)BsonTypeMapper.MapToDotNetValue(item.Value));

    private static string ToConfigString(BsonValue value) =>
        value.IsString ? value.AsString : value.ToJson();

    private static void ValidateConfigValue(
        CardDefinition definition,
        string key,
        JsonElement value)
    {
        var expected = definition.DefaultConfig[key];
        if (expected.IsNumeric && value.ValueKind != JsonValueKind.Number)
            throw new ApplicationValidationException($"Cấu hình '{key}' phải là số.");
        if (expected.IsString && value.ValueKind != JsonValueKind.String)
            throw new ApplicationValidationException($"Cấu hình '{key}' phải là chuỗi.");
        if (expected.IsBoolean && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ApplicationValidationException($"Cấu hình '{key}' phải là boolean.");

        if ((key is "card_use_count_max" or "cdSteal" or "mapPieceViewLimit" or "penaltyPoints") &&
            (!value.TryGetInt32(out var positiveInteger) || positiveInteger <= 0))
            throw new ApplicationValidationException($"Cấu hình '{key}' phải là số nguyên dương.");
        if ((key is "timeBetweenUseMinutes" or "cdSelfPenalty" or "failurePenalty") &&
            (!value.TryGetInt32(out var nonNegativeInteger) || nonNegativeInteger < 0))
            throw new ApplicationValidationException($"Cấu hình '{key}' phải là số nguyên không âm.");
        if (key == "rewardMultiplier" && value.GetDouble() < 0)
            throw new ApplicationValidationException("Reward multiplier không được âm.");
        if (key == "scoreMultiplier" && value.GetDouble() < 1)
            throw new ApplicationValidationException("Score multiplier phải lớn hơn hoặc bằng 1.");

        if (key == "requiredBoothType" &&
            !string.Equals(value.GetString(), expected.AsString, StringComparison.Ordinal))
            throw new ApplicationValidationException("Loại booth bắt buộc là invariant của card.");
        if ((key is "qualificationMode" or "consumeWhen") &&
            !string.Equals(value.GetString(), expected.AsString, StringComparison.Ordinal))
            throw new ApplicationValidationException($"Cấu hình '{key}' là invariant của card.");
    }

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
