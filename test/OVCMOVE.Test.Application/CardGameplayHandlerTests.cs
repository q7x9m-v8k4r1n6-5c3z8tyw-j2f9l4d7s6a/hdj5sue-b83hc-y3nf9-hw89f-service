using MongoDB.Bson;
using OVCMOVE.Application.Common;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class CardGameplayHandlerTests
{
    [Fact]
    public void Catalog_contains_only_the_current_gameplay_scope()
    {
        var ids = CardCatalog.All.Select(item => item.CardId).ToHashSet();

        Assert.Equal(7, ids.Count);
        Assert.Contains(CardIds.Overclock, ids);
        Assert.Contains(CardIds.Cupid, ids);
        Assert.Contains(CardIds.Engineer, ids);
        Assert.Contains(CardIds.Athlete, ids);
        Assert.Contains(CardIds.Revive, ids);
        Assert.Contains(CardIds.Swap, ids);
        Assert.Contains(CardIds.Trap, ids);
    }

    [Fact]
    public async Task Overclock_rejects_duplicate_opponent_predictions()
    {
        var target = Guid.NewGuid();
        var handler = new OverclockCardUseHandler();
        var context = Context(
            CardIds.Overclock,
            new BsonDocument("predictions", new BsonArray
            {
                Prediction(target, Guid.NewGuid()),
                Prediction(target, Guid.NewGuid())
            }));

        await Assert.ThrowsAsync<ApplicationValidationException>(() =>
            handler.PrepareAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task Cupid_creates_one_effect_for_the_next_finalized_result()
    {
        var target = Guid.NewGuid();
        var handler = new CupidCardUseHandler();

        var plan = await handler.PrepareAsync(
            Context(CardIds.Cupid, new BsonDocument("targetTeamId", target.ToString())),
            CancellationToken.None);

        Assert.Equal(CardUseStatus.Active, plan.Status);
        Assert.True(plan.ConsumeNow);
        Assert.Equal(CardEffectEventCodes.BoothResultFinalized, plan.Effect?.TriggerEventCode);
        Assert.Equal(target.ToString(), plan.Effect?.TargetTeamId);
    }

    [Theory]
    [InlineData(CardIds.Engineer, "intellectual", "any_success")]
    [InlineData(CardIds.Athlete, "physical", "score_equals_booth_max")]
    public async Task Booth_bonus_cards_create_typed_waiting_effects(
        string cardId,
        string expectedBoothType,
        string expectedQualification)
    {
        ICardUseHandler handler = cardId == CardIds.Engineer
            ? new EngineerCardUseHandler()
            : new AthleteCardUseHandler();

        var plan = await handler.PrepareAsync(Context(cardId), CancellationToken.None);

        Assert.Equal(CardUseStatus.Active, plan.Status);
        Assert.Equal(expectedBoothType, plan.Effect?.Data["requiredBoothType"].AsString);
        Assert.Equal(expectedQualification, plan.Effect?.Data["qualificationMode"].AsString);
    }

    [Fact]
    public async Task Swap_is_resolved_immediately_and_requests_both_notifications()
    {
        var handler = new SwapCardUseHandler();
        var plan = await handler.PrepareAsync(
            Context(CardIds.Swap, new BsonDocument("targetTeamId", Guid.NewGuid().ToString())),
            CancellationToken.None);

        Assert.Equal(CardUseStatus.Resolved, plan.Status);
        Assert.Null(plan.Effect);
        Assert.Equal(2, plan.Notifications?.Count);
    }

    private static CardUseContext Context(string cardId, BsonDocument? inputs = null)
    {
        var definition = CardCatalog.Get(cardId);
        return new CardUseContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new CardInventoryState
            {
                CardId = cardId,
                CardConfig = definition.DefaultConfig.DeepClone().AsBsonDocument
            },
            new TeamCardState
            {
                CardInfo = new TeamCardInfo
                {
                    CardId = cardId,
                    CardInstanceId = Guid.NewGuid().ToString(),
                    CardUseCountRemain = 3
                }
            },
            Guid.NewGuid().ToString(),
            inputs ?? new BsonDocument(),
            DateTime.UtcNow);
    }

    private static BsonDocument Prediction(Guid teamId, Guid boothId) => new()
    {
        ["targetTeamId"] = teamId.ToString(),
        ["boothId"] = boothId.ToString()
    };
}
