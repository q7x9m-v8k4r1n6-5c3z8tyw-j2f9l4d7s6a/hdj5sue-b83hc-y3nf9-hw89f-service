using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.CardEffects;

public sealed class CardEffectWorkflowExecutor
{
    private readonly IReadOnlyDictionary<string, ICardEffectConditionHandler>
        _conditionHandlers;
    private readonly IReadOnlyDictionary<string, ICardEffectActionHandler>
        _actionHandlers;
    private readonly CardEffectWorkflowValidator _validator;

    public CardEffectWorkflowExecutor(
        IEnumerable<ICardEffectConditionHandler> conditionHandlers,
        IEnumerable<ICardEffectActionHandler> actionHandlers,
        CardEffectWorkflowValidator validator)
    {
        _conditionHandlers = ToUniqueDictionary(
            conditionHandlers,
            handler => handler.Type,
            "condition");
        _actionHandlers = ToUniqueDictionary(
            actionHandlers,
            handler => handler.Type,
            "action");
        _validator = validator;
    }

    public async Task<CardEffectExecutionResult> ExecuteAsync(
        CardEffectWorkflowDefinition definition,
        CardEffectEvent gameEvent,
        CardEffectRuntimeState state,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validationErrors = _validator.Validate(
            definition,
            _conditionHandlers.Keys.ToHashSet(StringComparer.Ordinal),
            _actionHandlers.Keys.ToHashSet(StringComparer.Ordinal));
        if (validationErrors.Count > 0)
        {
            throw new ApplicationValidationException(
                string.Join(" ", validationErrors));
        }

        if (state.Status != CardEffectStatus.Active ||
            state.RemainingUses <= 0 ||
            !string.Equals(
                definition.TriggerType,
                gameEvent.Type,
                StringComparison.Ordinal))
        {
            return Ignored(state);
        }

        if (state.ExpiresAt is not null &&
            state.ExpiresAt <= gameEvent.OccurredAt)
        {
            return Ignored(state with { Status = CardEffectStatus.Expired });
        }

        var context = new CardEffectExecutionContext(
            definition,
            gameEvent,
            state);
        var trace = new List<CardEffectTraceEntry>();

        foreach (var condition in definition.Conditions)
        {
            var passed = await _conditionHandlers[condition.Type]
                .EvaluateAsync(context, condition, cancellationToken);
            trace.Add(new CardEffectTraceEntry(
                "condition",
                condition.Type,
                passed,
                passed ? "Condition passed." : "Condition did not match."));

            if (!passed)
            {
                return new CardEffectExecutionResult
                {
                    Outcome = CardEffectOutcome.ConditionsNotMet,
                    State = state,
                    Trace = trace
                };
            }
        }

        var cancelCurrentAction = false;
        foreach (var action in definition.Actions)
        {
            var result = await _actionHandlers[action.Type]
                .ExecuteAsync(context, action, cancellationToken);
            trace.Add(new CardEffectTraceEntry(
                "action",
                action.Type,
                result.Succeeded,
                result.Message));

            if (!result.Succeeded)
            {
                return new CardEffectExecutionResult
                {
                    Outcome = CardEffectOutcome.ActionFailed,
                    CancelCurrentAction = cancelCurrentAction,
                    State = state,
                    Trace = trace
                };
            }

            cancelCurrentAction |= result.CancelCurrentAction;
            if (result.Stop)
            {
                break;
            }
        }

        var nextState = ApplySuccessfulExecutionPolicy(definition.Policy, state);
        return new CardEffectExecutionResult
        {
            Outcome = CardEffectOutcome.Succeeded,
            CancelCurrentAction = cancelCurrentAction,
            State = nextState,
            Trace = trace
        };
    }

    private static CardEffectRuntimeState ApplySuccessfulExecutionPolicy(
        CardEffectExecutionPolicy policy,
        CardEffectRuntimeState state)
    {
        if (policy.ConsumeWhen != CardEffectConsumeWhen.ActionSucceeded)
        {
            return state;
        }

        var remainingUses = Math.Max(0, state.RemainingUses - 1);
        return state with
        {
            RemainingUses = remainingUses,
            Status = remainingUses == 0
                ? CardEffectStatus.Consumed
                : CardEffectStatus.Active
        };
    }

    private static CardEffectExecutionResult Ignored(
        CardEffectRuntimeState state) =>
        new()
        {
            Outcome = CardEffectOutcome.Ignored,
            State = state
        };

    private static IReadOnlyDictionary<string, THandler> ToUniqueDictionary<THandler>(
        IEnumerable<THandler> handlers,
        Func<THandler, string> typeSelector,
        string handlerKind)
        where THandler : notnull
    {
        var result = new Dictionary<string, THandler>(StringComparer.Ordinal);
        foreach (var handler in handlers)
        {
            var type = typeSelector(handler);
            if (!result.TryAdd(type, handler))
            {
                throw new InvalidOperationException(
                    $"Duplicate card-effect {handlerKind} handler: {type}.");
            }
        }

        return result;
    }
}

