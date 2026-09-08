using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public interface ICardShopService
{
    Task<CardShopStateResponse> GetStateAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<TeamCardShopResponse> GetTeamShopAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default);
    Task<CardShopStateResponse> SetStoreOpenAsync(Guid raceId, bool isOpen, CancellationToken cancellationToken = default);
    Task<CardShopStateResponse> SetMaxDataPatchPerTeamAsync(Guid raceId, int maximum, CancellationToken cancellationToken = default);
    Task<CardInventoryResponse> SetPriceAsync(Guid raceId, string cardId, int price, CancellationToken cancellationToken = default);
    Task<CardPurchaseResponse> PurchaseAsync(Guid raceId, Guid teamId, string cardId, Guid purchaseId, CancellationToken cancellationToken = default);
}

public sealed class CardShopService(
    IRaceCardRepository cardRepository,
    IRaceRepository raceRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IBoothNotificationService notificationService,
    ILogger<CardShopService> logger) : ICardShopService
{
    private const string PurchaseReason = "shop_purchase";
    private const string PurchaseEventCode = "card-purchase";
    private const string PurchaseEventName = "Mua Data Patch";
    private const string PurchaseReasonCode = "card_purchase";
    private static readonly TimeSpan AbandonedReservationAfter = TimeSpan.FromMinutes(2);

    public async Task<CardShopStateResponse> GetStateAsync(
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        ToState(await GetDocumentAsync(raceId, cancellationToken));

    public async Task<TeamCardShopResponse> GetTeamShopAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTeamInRaceAsync(raceId, teamId, cancellationToken);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var purchasedCount = CountPurchasedDataPatches(document, teamId);
        var cards = CardCatalog.All
            .Where(definition => definition.CardType == CardTypes.DataPatch)
            .Select(definition =>
            {
                var inventory = FindInventory(document, definition.CardId);
                return new CardShopItemResponse(
                    definition.CardId,
                    definition.CardName,
                    definition.Description,
                    inventory.Price,
                    inventory.RemainingStock,
                    definition.Usage,
                    definition.Inputs);
            })
            .ToArray();

        return new TeamCardShopResponse(
            document.StoreOpen,
            document.MaxDataPatchPerTeam,
            purchasedCount,
            Math.Max(0, document.MaxDataPatchPerTeam - purchasedCount),
            cards);
    }

    public async Task<CardShopStateResponse> SetStoreOpenAsync(
        Guid raceId,
        bool isOpen,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        if (document.StoreOpen == isOpen) return ToState(document);
        document.StoreOpen = isOpen;
        await cardRepository.ReplaceAsync(document, cancellationToken);
        return ToState(document);
    }

    public async Task<CardShopStateResponse> SetMaxDataPatchPerTeamAsync(
        Guid raceId,
        int maximum,
        CancellationToken cancellationToken = default)
    {
        if (maximum is < 1 or > 20)
            throw new ApplicationValidationException("Giới hạn Data Patch mỗi đội phải từ 1 đến 20.");

        var document = await GetDocumentAsync(raceId, cancellationToken);
        if (document.StoreOpen)
            throw new ApplicationConflictException("Hãy đóng cửa hàng trước khi đổi giới hạn mua.");

        var largestPurchaseCount = document.Teams
            .Select(team => team.Cards.Count(IsCountedPurchase))
            .DefaultIfEmpty(0)
            .Max();
        if (maximum < largestPurchaseCount)
            throw new ApplicationConflictException(
                $"Đã có đội mua {largestPurchaseCount} Data Patch; không thể đặt giới hạn thấp hơn.");

        document.MaxDataPatchPerTeam = maximum;
        await cardRepository.ReplaceAsync(document, cancellationToken);
        return ToState(document);
    }

    public async Task<CardInventoryResponse> SetPriceAsync(
        Guid raceId,
        string cardId,
        int price,
        CancellationToken cancellationToken = default)
    {
        if (price is < 1 or > 1_000_000)
            throw new ApplicationValidationException("Giá Data Patch phải từ 1 đến 1.000.000 CD.");

        var definition = CardCatalog.Get(cardId);
        if (definition.CardType != CardTypes.DataPatch)
            throw new ApplicationValidationException("Core Chip không được bán trong cửa hàng.");

        var document = await GetDocumentAsync(raceId, cancellationToken);
        if (document.StoreOpen)
            throw new ApplicationConflictException("Hãy đóng cửa hàng trước khi đổi giá.");

        var inventory = FindInventory(document, definition.CardId);
        inventory.Price = price;
        await cardRepository.ReplaceAsync(document, cancellationToken);
        return ToInventoryResponse(definition, inventory);
    }

    public async Task<CardPurchaseResponse> PurchaseAsync(
        Guid raceId,
        Guid teamId,
        string cardId,
        Guid purchaseId,
        CancellationToken cancellationToken = default)
    {
        if (purchaseId == Guid.Empty)
            throw new ApplicationValidationException("purchaseId là bắt buộc.");
        await EnsureTeamInRaceAsync(raceId, teamId, cancellationToken);

        var definition = CardCatalog.Get(cardId);
        if (definition.CardType != CardTypes.DataPatch)
            throw new ApplicationValidationException("Chỉ Data Patch được mua trong cửa hàng.");

        var teamUser = await userRepository.GetByIdAsync(teamId, cancellationToken)
            ?? throw new ApplicationValidationException("Không tìm thấy thông tin team đang hoạt động.");
        var teamName = GetCanonicalTeamName(teamUser);
        var reservation = await ReserveAsync(
            raceId, teamId, teamName, definition, purchaseId, cancellationToken);
        var eventId = CreateEventId(raceId, purchaseId);

        var purchaseLog = await GetPurchaseLogAsync(
            raceId, teamId, eventId, reservation.Price, cancellationToken);
        var chargedNow = false;
        if (purchaseLog is null && !reservation.Created &&
            DateTime.UtcNow - reservation.ReservedAt < AbandonedReservationAfter)
            throw new ApplicationConflictException(
                "Giao dịch mua card đang được xử lý. Hãy retry cùng purchaseId sau ít phút.");

        if (purchaseLog is null)
        {
            var commitStarted = false;
            try
            {
                await unitOfWork.BeginAsync(cancellationToken);
                var mutation = await raceRepository.TryDebitRaceTeamScoreAsync(
                    raceId,
                    teamId,
                    reservation.Price,
                    teamId.ToString(),
                    DateTime.UtcNow,
                    cancellationToken);
                if (mutation is null)
                    throw new ApplicationConflictException("Đội không đủ CD để mua Data Patch này.");

                purchaseLog = CreatePurchaseLog(
                    raceId, teamId, eventId, definition, reservation.Price, mutation);
                await raceRepository.CreateScoringLogAsync(purchaseLog, cancellationToken);
                commitStarted = true;
                await unitOfWork.CommitAsync(CancellationToken.None);
                chargedNow = true;
            }
            catch (Exception exception)
            {
                try
                {
                    await unitOfWork.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    logger.LogWarning(rollbackException, "Could not roll back card purchase {PurchaseId}.", purchaseId);
                }

                if (!commitStarted)
                    await ReleaseReservationAsync(raceId, purchaseId, CancellationToken.None);
                else
                    logger.LogCritical(
                        exception,
                        "SQL commit outcome is unknown for card purchase {PurchaseId}, event {EventId}; Mongo reservation was retained.",
                        purchaseId,
                        eventId);
                throw;
            }
        }

        var completion = await CompleteReservationAsync(
            raceId, purchaseId, reservation.RemainingStock, CancellationToken.None);
        var message = completion.Completed
            ? "Mua Data Patch thành công."
            : "CD đã được trừ nhưng card chưa đồng bộ. Hãy retry cùng purchaseId hoặc liên hệ admin.";

        try
        {
            if (chargedNow)
                await notificationService.NotifyRaceScoreChangedAsync(
                    raceId, teamId, -reservation.Price, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Card purchase score notification failed for {PurchaseId}.", purchaseId);
            message += " Điểm realtime chưa đồng bộ; vui lòng tải lại.";
        }

        return new CardPurchaseResponse(
            purchaseId.ToString(),
            eventId,
            reservation.CardInstanceId,
            definition.CardId,
            reservation.Price,
            purchaseLog.ScoreBefore,
            purchaseLog.ScoreAfter,
            completion.RemainingStock,
            completion.Completed ? CardStatus.Received : CardStatus.PendingPurchase,
            message);
    }

    private async Task<PurchaseReservation> ReserveAsync(
        Guid raceId,
        Guid teamId,
        string teamName,
        CardDefinition definition,
        Guid purchaseId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var document = await GetDocumentAsync(raceId, cancellationToken);
            var existing = FindByPurchaseId(document, purchaseId);
            if (existing is not null)
            {
                if (existing.Value.Team.TeamId != teamId.ToString() ||
                    !SameCard(existing.Value.Card, definition.CardId))
                    throw new ApplicationConflictException("purchaseId đã được dùng cho giao dịch khác.");
                if (existing.Value.Card.Status == CardStatus.Deleted)
                    throw new ApplicationConflictException("Giao dịch mua card này đã bị hủy.");
                return new PurchaseReservation(
                    existing.Value.Card.CardInfo.CardInstanceId,
                    existing.Value.Card.PurchasePrice ?? FindInventory(document, definition.CardId).Price,
                    existing.Value.Card.ReceivedAt,
                    FindInventory(document, definition.CardId).RemainingStock,
                    false);
            }

            if (!document.StoreOpen)
                throw new ApplicationConflictException("Cửa hàng Data Patch hiện đang đóng.");
            var inventory = FindInventory(document, definition.CardId);
            if (inventory.RemainingStock <= 0)
                throw new ApplicationConflictException("Data Patch đã hết hàng.");
            if (inventory.Price <= 0)
                throw new ApplicationConflictException("Data Patch chưa có giá hợp lệ.");

            var purchasedCount = CountPurchasedDataPatches(document, teamId);
            if (purchasedCount >= document.MaxDataPatchPerTeam)
                throw new ApplicationConflictException("Đội đã đạt giới hạn Data Patch trong race.");

            var team = document.Teams.FirstOrDefault(item => item.TeamId == teamId.ToString());
            if (team is null)
            {
                team = new RaceCardTeamState { TeamId = teamId.ToString(), TeamName = teamName };
                document.Teams.Add(team);
            }
            else
            {
                team.TeamName = teamName;
            }

            if (definition.CardId == CardIds.Trap && team.Cards.Any(card =>
                    SameCard(card, CardIds.Trap) && card.Status != CardStatus.Deleted))
                throw new ApplicationConflictException("Mỗi team chỉ được nhận một Trap trong race.");

            var now = DateTime.UtcNow;
            var card = new TeamCardState
            {
                CardInfo = new TeamCardInfo
                {
                    CardInstanceId = Guid.NewGuid().ToString(),
                    CardId = definition.CardId,
                    CardUseCountRemain = GetUseCount(inventory, definition)
                },
                ReceivedAt = now,
                ReceiveReason = PurchaseReason,
                PurchaseId = purchaseId.ToString(),
                PurchasePrice = inventory.Price,
                Status = CardStatus.PendingPurchase
            };
            team.Cards.Add(card);
            inventory.RemainingStock--;

            try
            {
                await cardRepository.ReplaceAsync(document, cancellationToken);
                return new PurchaseReservation(
                    card.CardInfo.CardInstanceId,
                    inventory.Price,
                    now,
                    inventory.RemainingStock,
                    true);
            }
            catch (ApplicationConflictException) when (attempt < 3)
            {
                // Reload the race document and re-evaluate stock and purchase limits.
            }
        }

        throw new ApplicationConflictException("Kho card vừa thay đổi. Vui lòng thử lại.");
    }

    private async Task<ScoringLog?> GetPurchaseLogAsync(
        Guid raceId,
        Guid teamId,
        string eventId,
        int expectedPrice,
        CancellationToken cancellationToken)
    {
        var logs = await raceRepository.GetScoringLogsByEventIdAsync(
            raceId, eventId, cancellationToken);
        if (logs.Count == 0) return null;

        var purchaseLog = logs.FirstOrDefault(log =>
            log.TeamId == teamId && log.EventCode == PurchaseEventCode);
        if (purchaseLog is null || purchaseLog.Delta != -expectedPrice)
            throw new ApplicationConflictException("purchaseId đã được dùng cho thay đổi điểm khác.");
        return purchaseLog;
    }

    private async Task<(bool Completed, int RemainingStock)> CompleteReservationAsync(
        Guid raceId,
        Guid purchaseId,
        int fallbackRemainingStock,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var document = await GetDocumentAsync(raceId, cancellationToken);
                var existing = FindByPurchaseId(document, purchaseId)
                    ?? throw new ApplicationConflictException("Không tìm thấy card đang chờ đồng bộ.");
                var inventory = FindInventory(document, existing.Card.CardInfo.CardId);
                if (existing.Card.Status == CardStatus.Received)
                    return (true, inventory.RemainingStock);
                if (existing.Card.Status != CardStatus.PendingPurchase)
                    throw new ApplicationConflictException("Trạng thái giao dịch mua card không hợp lệ.");

                existing.Card.Status = CardStatus.Received;
                await cardRepository.ReplaceAsync(document, cancellationToken);
                return (true, inventory.RemainingStock);
            }
            catch (Exception exception) when (
                exception is ApplicationConflictException or
                    ApplicationServiceUnavailableException or MongoException)
            {
                logger.LogWarning(exception, "Could not complete Mongo card purchase {PurchaseId} on attempt {Attempt}/3.", purchaseId, attempt);
                if (attempt == 3) break;
            }
        }

        logger.LogCritical(
            "SQL card purchase committed but Mongo purchase {PurchaseId} requires reconciliation.",
            purchaseId);
        return (false, fallbackRemainingStock);
    }

    private async Task ReleaseReservationAsync(
        Guid raceId,
        Guid purchaseId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var document = await GetDocumentAsync(raceId, cancellationToken);
                var existing = FindByPurchaseId(document, purchaseId);
                if (existing is null || existing.Value.Card.Status != CardStatus.PendingPurchase) return;

                existing.Value.Team.Cards.Remove(existing.Value.Card);
                FindInventory(document, existing.Value.Card.CardInfo.CardId).RemainingStock++;
                await cardRepository.ReplaceAsync(document, cancellationToken);
                return;
            }
            catch (Exception exception) when (
                exception is ApplicationConflictException or
                    ApplicationServiceUnavailableException or MongoException)
            {
                logger.LogWarning(
                    exception,
                    "Could not release failed Mongo card purchase reservation {PurchaseId} on attempt {Attempt}/3.",
                    purchaseId,
                    attempt);
                if (attempt == 3) break;
            }
        }

        logger.LogCritical("Could not release failed Mongo card purchase reservation {PurchaseId}.", purchaseId);
    }

    private static ScoringLog CreatePurchaseLog(
        Guid raceId,
        Guid teamId,
        string eventId,
        CardDefinition definition,
        int price,
        OVCMOVE.Application.Features.Races.Common.RaceTeamScoreMutation mutation)
    {
        var now = DateTime.UtcNow;
        return new ScoringLog
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventCode = PurchaseEventCode,
            EventName = PurchaseEventName,
            RaceId = raceId,
            TeamId = teamId,
            ActorId = teamId,
            Delta = -price,
            ScoreBefore = mutation.ScoreBefore,
            ScoreAfter = mutation.ScoreAfter,
            ReasonCode = PurchaseReasonCode,
            Reason = $"Mua Data Patch {definition.CardName}",
            CreatedBy = teamId.ToString(),
            CreatedAt = now,
            ModifiedBy = teamId.ToString(),
            ModifiedAt = now
        };
    }

    private async Task<RaceCardDocument> GetDocumentAsync(
        Guid raceId,
        CancellationToken cancellationToken)
    {
        if (raceId == Guid.Empty)
            throw new ApplicationValidationException("raceId là bắt buộc.");
        try
        {
            return await cardRepository.GetOrCreateAsync(raceId, cancellationToken);
        }
        catch (MongoAuthenticationException exception)
        {
            throw new ApplicationServiceUnavailableException("Không thể xác thực với MongoDB.", exception);
        }
        catch (MongoConnectionException exception)
        {
            throw new ApplicationServiceUnavailableException("Không thể kết nối MongoDB để tải cửa hàng card.", exception);
        }
    }

    private async Task EnsureTeamInRaceAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        if (raceId == Guid.Empty || teamId == Guid.Empty ||
            !await raceRepository.IsTeamInRaceAsync(raceId, teamId, cancellationToken))
            throw new ApplicationValidationException("Team không tham gia race này.");
    }

    private static (RaceCardTeamState Team, TeamCardState Card)? FindByPurchaseId(
        RaceCardDocument document,
        Guid purchaseId)
    {
        var value = purchaseId.ToString();
        foreach (var team in document.Teams)
        {
            var card = team.Cards.FirstOrDefault(item =>
                string.Equals(item.PurchaseId, value, StringComparison.OrdinalIgnoreCase));
            if (card is not null) return (team, card);
        }
        return null;
    }

    private static int CountPurchasedDataPatches(RaceCardDocument document, Guid teamId)
    {
        var team = document.Teams.FirstOrDefault(item => item.TeamId == teamId.ToString());
        return team?.Cards.Count(IsCountedPurchase) ?? 0;
    }

    private static bool IsCountedPurchase(TeamCardState card) =>
        card.Status != CardStatus.Deleted &&
        card.ReceiveReason == PurchaseReason &&
        CardCatalog.TryGet(card.CardInfo.CardId)?.CardType == CardTypes.DataPatch;

    private static CardInventoryResponse ToInventoryResponse(
        CardDefinition definition,
        CardInventoryState inventory) =>
        new(
            definition.CardId,
            definition.CardName,
            definition.CardType,
            definition.Description,
            inventory.Price,
            inventory.RemainingStock,
            definition.Usage,
            definition.Inputs,
            inventory.CardConfig.ToDictionary(
                item => item.Name,
                item => (object?)BsonTypeMapper.MapToDotNetValue(item.Value)));

    private static int GetUseCount(CardInventoryState inventory, CardDefinition definition) =>
        inventory.CardConfig.GetInt(
            "card_use_count_max",
            definition.DefaultConfig.GetInt("card_use_count_max", 1));

    private static bool SameCard(TeamCardState card, string cardId) =>
        card.CardInfo.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase);

    private static CardInventoryState FindInventory(RaceCardDocument document, string cardId) =>
        document.Inventory.FirstOrDefault(item => item.CardId.Equals(cardId, StringComparison.OrdinalIgnoreCase))
        ?? throw new ApplicationNotFoundException($"Không tìm thấy card '{cardId}' trong kho.");

    private static string GetCanonicalTeamName(User team) =>
        new[] { team.DisplayName, team.Username, team.LinkedEmail }
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? team.Id.ToString();

    private static string CreateEventId(Guid raceId, Guid purchaseId) =>
        $"card-purchase:{raceId:N}:{purchaseId:N}";

    private static CardShopStateResponse ToState(RaceCardDocument document) =>
        new(document.StoreOpen, document.MaxDataPatchPerTeam);

    private sealed record PurchaseReservation(
        string CardInstanceId,
        int Price,
        DateTime ReservedAt,
        int RemainingStock,
        bool Created);
}
