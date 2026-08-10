using System.Text.Json;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.CardEffects;

namespace OVCMOVE.Test.Application;

public sealed class CardEffectWorkflowExecutorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Shield_CancelsOnlyTheFirstMatchingAction()
    {
        var cancelHandler = new CancelCurrentActionHandler();
        var executor = Executor(
            [new TargetIsEffectOwnerHandler(), new ActionTypeInHandler()],
            [cancelHandler]);
        var definition = ShieldDefinition();
        var state = ActiveShield();
        var attemptedSteal = AttemptedAction(
            targetTeamId: state.OwnerTeamId,
            actionType: "steal_score");

        var first = await executor.ExecuteAsync(
            definition,
            attemptedSteal,
            state);
        var second = await executor.ExecuteAsync(
            definition,
            attemptedSteal with { EventId = Guid.NewGuid() },
            first.State);

        Assert.Equal(CardEffectOutcome.Succeeded, first.Outcome);
        Assert.True(first.CancelCurrentAction);
        Assert.Equal(0, first.State.RemainingUses);
        Assert.Equal(CardEffectStatus.Consumed, first.State.Status);
        Assert.Equal(CardEffectOutcome.Ignored, second.Outcome);
        Assert.False(second.CancelCurrentAction);
        Assert.Equal(1, cancelHandler.ExecutionCount);
    }

    [Fact]
    public async Task Shield_DoesNotConsumeWhenTargetIsAnotherTeam()
    {
        var executor = Executor(
            [new TargetIsEffectOwnerHandler(), new ActionTypeInHandler()],
            [new CancelCurrentActionHandler()]);
        var state = ActiveShield();

        var result = await executor.ExecuteAsync(
            ShieldDefinition(),
            AttemptedAction(Guid.NewGuid(), "steal_score"),
            state);

        Assert.Equal(CardEffectOutcome.ConditionsNotMet, result.Outcome);
        Assert.Equal(1, result.State.RemainingUses);
        Assert.Equal(CardEffectStatus.Active, result.State.Status);
    }

    [Fact]
    public async Task Executor_RejectsUnregisteredActionTypes()
    {
        var executor = Executor(
            [new TargetIsEffectOwnerHandler(), new ActionTypeInHandler()],
            []);

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => executor.ExecuteAsync(
                ShieldDefinition(),
                AttemptedAction(Guid.NewGuid(), "trap"),
                ActiveShield()));

        Assert.Contains(
            "Unsupported action type: action.cancel_current_action",
            exception.Message);
    }

    private static CardEffectWorkflowExecutor Executor(
        IEnumerable<ICardEffectConditionHandler> conditions,
        IEnumerable<ICardEffectActionHandler> actions) =>
        new(conditions, actions, new CardEffectWorkflowValidator());

    private static CardEffectWorkflowDefinition ShieldDefinition() =>
        new()
        {
            WorkflowVersionId = Guid.NewGuid(),
            Version = 1,
            TriggerType = "game_action.attempted",
            Conditions =
            [
                new CardEffectStepDefinition
                {
                    Type = "condition.target_is_effect_owner"
                },
                new CardEffectStepDefinition
                {
                    Type = "condition.action_type_in",
                    Config = JsonSerializer.SerializeToElement(new
                    {
                        allowed = new[] { "steal_score", "trap" }
                    })
                }
            ],
            Actions =
            [
                new CardEffectStepDefinition
                {
                    Type = "action.cancel_current_action"
                }
            ],
            Policy = new CardEffectExecutionPolicy
            {
                MaximumTriggers = 1,
                ConsumeWhen = CardEffectConsumeWhen.ActionSucceeded
            }
        };

    private static CardEffectRuntimeState ActiveShield() =>
        new()
        {
            EffectInstanceId = Guid.NewGuid(),
            OwnerTeamId = Guid.NewGuid(),
            RemainingUses = 1,
            Status = CardEffectStatus.Active,
            ExpiresAt = Now.AddHours(1)
        };

    private static CardEffectEvent AttemptedAction(
        Guid targetTeamId,
        string actionType) =>
        new()
        {
            EventId = Guid.NewGuid(),
            Type = "game_action.attempted",
            RaceId = Guid.NewGuid(),
            ActorTeamId = Guid.NewGuid(),
            TargetTeamId = targetTeamId,
            OccurredAt = Now,
            Data = JsonSerializer.SerializeToElement(new { actionType })
        };

    private sealed class TargetIsEffectOwnerHandler : ICardEffectConditionHandler
    {
        public string Type => "condition.target_is_effect_owner";

        public Task<bool> EvaluateAsync(
            CardEffectExecutionContext context,
            CardEffectStepDefinition condition,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                context.Event.TargetTeamId == context.State.OwnerTeamId);
        }
    }

    private sealed class ActionTypeInHandler : ICardEffectConditionHandler
    {
        public string Type => "condition.action_type_in";

        public Task<bool> EvaluateAsync(
            CardEffectExecutionContext context,
            CardEffectStepDefinition condition,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actionType = context.Event.Data
                .GetProperty("actionType")
                .GetString();
            var allowed = condition.Config
                .GetProperty("allowed")
                .EnumerateArray()
                .Select(value => value.GetString());

            return Task.FromResult(allowed.Contains(actionType));
        }
    }

    private sealed class CancelCurrentActionHandler : ICardEffectActionHandler
    {
        public string Type => "action.cancel_current_action";
        public int ExecutionCount { get; private set; }

        public Task<CardEffectActionResult> ExecuteAsync(
            CardEffectExecutionContext context,
            CardEffectStepDefinition action,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExecutionCount++;
            return Task.FromResult(CardEffectActionResult.Success(
                cancelCurrentAction: true,
                message: "The attempted game action was cancelled."));
        }
    }
}

