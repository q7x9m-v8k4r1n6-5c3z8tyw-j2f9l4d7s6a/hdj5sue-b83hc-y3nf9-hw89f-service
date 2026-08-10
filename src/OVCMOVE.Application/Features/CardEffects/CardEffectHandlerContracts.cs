namespace OVCMOVE.Application.Features.CardEffects;

public interface ICardEffectConditionHandler
{
    string Type { get; }

    Task<bool> EvaluateAsync(
        CardEffectExecutionContext context,
        CardEffectStepDefinition condition,
        CancellationToken cancellationToken);
}

public interface ICardEffectActionHandler
{
    string Type { get; }

    Task<CardEffectActionResult> ExecuteAsync(
        CardEffectExecutionContext context,
        CardEffectStepDefinition action,
        CancellationToken cancellationToken);
}

public sealed record CardEffectActionResult
{
    public bool Succeeded { get; init; }
    public bool CancelCurrentAction { get; init; }
    public bool Stop { get; init; }
    public string? Message { get; init; }

    public static CardEffectActionResult Success(
        bool cancelCurrentAction = false,
        bool stop = false,
        string? message = null) =>
        new()
        {
            Succeeded = true,
            CancelCurrentAction = cancelCurrentAction,
            Stop = stop,
            Message = message
        };

    public static CardEffectActionResult Failure(string message) =>
        new()
        {
            Succeeded = false,
            Message = message
        };
}

