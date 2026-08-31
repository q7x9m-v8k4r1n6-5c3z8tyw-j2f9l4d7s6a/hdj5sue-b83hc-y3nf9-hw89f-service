using System.Globalization;
using MongoDB.Driver;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public sealed class RaceCardService(
    IRaceCardRepository repository) : IRaceCardService
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
        var card = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        return document.Teams
            .Where(item => item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.ReceivedAt)
            .Select(ToTeamResponse)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TeamCardResponse>> GetTeamCardsAsync(
        Guid raceId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentAsync(raceId, cancellationToken);
        return document.Teams
            .Where(item => item.TeamId == teamId.ToString() && item.Status != CardStatus.Deleted)
            .OrderByDescending(item => item.ReceivedAt)
            .Select(item => ToTeamCardResponse(item, document))
            .ToArray();
    }

    public async Task<TeamCardResponse> GetTeamCardAsync(
        Guid raceId,
        Guid teamId,
        string cardId,
        CancellationToken cancellationToken = default)
    {
        var card = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var assignment = document.Teams.LastOrDefault(item =>
            item.TeamId == teamId.ToString() &&
            item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase) &&
            item.Status != CardStatus.Deleted);
        return assignment is null
            ? throw new ApplicationNotFoundException("Không tìm thấy card trong kho của đội.")
            : ToTeamCardResponse(assignment, document);
    }

    public async Task SetStoreOpenAsync(Guid raceId, bool isOpen, CancellationToken cancellationToken = default)
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

        var document = await GetDocumentAsync(raceId, cancellationToken);
        ValidateQuantities(quantities);
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
        IReadOnlyDictionary<string, string> config,
        CancellationToken cancellationToken = default)
    {
        var card = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in card.DefaultConfig.Keys)
        {
            if (!config.TryGetValue(key, out var value))
                continue;
            if (key == "penaltyPoints" &&
                (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var points) || points <= 0))
                throw new ApplicationValidationException("Số điểm bị trừ phải là số nguyên dương.");
            normalized[key] = value.Trim();
        }

        document.CardConfigs[card.CardId] = normalized;
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
        var card = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        if (!document.StoreOpen)
            throw new ApplicationConflictException("Cửa hàng đang đóng.");
        if (document.Teams.Any(item =>
                item.TeamId == teamId.ToString() &&
                item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase) &&
                item.Status != CardStatus.Deleted))
            throw new ApplicationConflictException("Team đã có card này.");

        var inventory = document.Inventory.Single(item => item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase));
        if (inventory.RemainingStock <= 0)
            throw new ApplicationConflictException("Card đã hết trong kho.");

        inventory.RemainingStock--;
        var assignment = new RaceCardTeamState
        {
            TeamId = teamId.ToString(),
            TeamName = teamName.Trim(),
            CardId = card.CardId,
            CardName = card.CardName,
            ReceivedAt = DateTime.UtcNow,
            ReceiveReason = reason?.Trim() ?? string.Empty
        };
        document.Teams.Add(assignment);
        await repository.ReplaceAsync(document, cancellationToken);
        return ToTeamResponse(assignment);
    }

    public async Task DeleteAssignmentAsync(
        Guid raceId,
        string cardId,
        Guid teamId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ApplicationValidationException("Lý do xóa là bắt buộc.");
        var card = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        var assignment = document.Teams.LastOrDefault(item =>
            item.TeamId == teamId.ToString() &&
            item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase) &&
            item.Status == CardStatus.Received);
        if (assignment is null)
            throw new ApplicationConflictException("Chỉ được xóa card chưa sử dụng.");

        assignment.Status = CardStatus.Deleted;
        assignment.DeletedAt = DateTime.UtcNow;
        assignment.DeletedReason = reason.Trim();
        document.Inventory.Single(item => item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase)).RemainingStock++;
        await repository.ReplaceAsync(document, cancellationToken);
    }

    public async Task<CardUseResponse> UseAsync(
        Guid raceId,
        Guid teamId,
        string cardId,
        IReadOnlyDictionary<string, string> inputs,
        CancellationToken cancellationToken = default)
    {
        var card = CardCatalog.Get(cardId);
        var document = await GetDocumentAsync(raceId, cancellationToken);
        if (!document.StoreOpen)
            throw new ApplicationConflictException("Cửa hàng đang đóng.");
        var assignment = document.Teams.LastOrDefault(item =>
            item.TeamId == teamId.ToString() &&
            item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase) &&
            item.Status == CardStatus.Received);
        if (assignment is null)
            throw new ApplicationConflictException("Card không còn sẵn sàng để sử dụng.");

        if (card.CardId == CardIds.Trap)
        {
            if (!inputs.TryGetValue("boothId", out var boothId) || !Guid.TryParse(boothId, out _))
                throw new ApplicationValidationException("Trap cần input boothId hợp lệ.");
            if (document.Traps.Any(trap => trap.BoothId == boothId && trap.TriggeredAt is null))
                throw new ApplicationConflictException("Trạm này đã có bẫy đang hoạt động.");

            var penalty = GetPenalty(document, card);
            document.Traps.Add(new TrapState
            {
                BoothId = boothId,
                PlacedByTeamId = teamId.ToString(),
                PlacedAt = DateTime.UtcNow,
                PenaltyPoints = penalty
            });
        }

        assignment.Status = CardStatus.Used;
        assignment.UsedAt = DateTime.UtcNow;
        assignment.UsageInputs = inputs.ToDictionary(item => item.Key, item => item.Value);
        await repository.ReplaceAsync(document, cancellationToken);
        return new CardUseResponse(card.CardId, card.CardName, assignment.Status, assignment.UsedAt.Value, "Đã sử dụng card.");
    }

    public Task<TrapState?> TriggerTrapAsync(
        Guid raceId,
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default) =>
        repository.TryClaimTrapAsync(raceId, boothId, teamId, DateTime.UtcNow, cancellationToken);

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
            throw new ApplicationServiceUnavailableException(
                "Không thể xác thực với MongoDB. Vui lòng kiểm tra username/password, authSource và connection string.",
                exception);
        }
        catch (MongoConnectionException exception)
        {
            throw new ApplicationServiceUnavailableException(
                "Không thể kết nối MongoDB để tải dữ liệu card.",
                exception);
        }
    }

    private static CardInventoryResponse ToInventoryResponse(CardDefinition card, RaceCardDocument document)
    {
        var config = MergeConfig(card, document);
        var stock = document.Inventory.First(item => item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase)).RemainingStock;
        return new CardInventoryResponse(card.CardId, card.CardName, card.Description, card.Price, stock, card.Usage, card.Inputs, config);
    }

    private static CardTeamResponse ToTeamResponse(RaceCardTeamState item) => new(
        item.TeamId,
        item.TeamName,
        item.CardId,
        item.CardName,
        item.ReceivedAt,
        item.ReceiveReason,
        item.UsedAt,
        item.Status,
        item.Status == CardStatus.Received,
        item.DeletedAt,
        item.DeletedReason,
        item.UsageInputs);

    private static TeamCardResponse ToTeamCardResponse(RaceCardTeamState item, RaceCardDocument document)
    {
        var card = CardCatalog.Get(item.CardId);
        var config = MergeConfig(card, document);
        return new TeamCardResponse(card.CardId, card.CardName, card.Description, card.Usage, card.Inputs, config, item.ReceivedAt, item.ReceiveReason, item.UsedAt, item.Status);
    }

    private static IReadOnlyDictionary<string, string> MergeConfig(
        CardDefinition card,
        RaceCardDocument document)
    {
        var config = card.DefaultConfig.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        if (document.CardConfigs.TryGetValue(card.CardId, out var saved))
        {
            foreach (var (key, value) in saved)
                config[key] = value;
        }

        return config;
    }

    private static void ApplyQuantities(RaceCardDocument document, IReadOnlyDictionary<string, int> quantities)
    {
        ValidateQuantities(quantities);
        foreach (var (cardId, quantity) in quantities)
        {
            var card = CardCatalog.Get(cardId);
            document.Inventory.Single(item => item.CardId.Equals(card.CardId, StringComparison.OrdinalIgnoreCase)).RemainingStock += quantity;
        }
    }

    private static void ValidateQuantities(IReadOnlyDictionary<string, int> quantities)
    {
        foreach (var (cardId, quantity) in quantities)
        {
            _ = CardCatalog.Get(cardId);
            if (quantity < 0) throw new ApplicationValidationException("Số lượng nhập không được âm.");
        }
    }

    private static int GetPenalty(RaceCardDocument document, CardDefinition card)
    {
        var config = document.CardConfigs.TryGetValue(card.CardId, out var saved) ? saved : card.DefaultConfig;
        return int.TryParse(config.GetValueOrDefault("penaltyPoints"), out var points) && points > 0 ? points : 10;
    }
}
