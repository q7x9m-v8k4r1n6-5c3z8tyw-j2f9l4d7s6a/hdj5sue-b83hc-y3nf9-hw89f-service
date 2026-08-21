using System.Text.Json;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Application.Abstractions.Services;

/// <summary>
/// Integration boundary used by the future card backend to raise a card event
/// without knowing workflow persistence or runtime details.
/// </summary>
public interface ICardWorkflowDispatcher
{
    Task<WorkflowExecutionResultModel?> DispatchAsync(
        CardWorkflowEvent cardEvent,
        CancellationToken cancellationToken = default);
}

public sealed record CardWorkflowEvent(
    Guid RaceId,
    string CardKey,
    string TriggerType,
    string EventId,
    Guid? ActorTeamId,
    Guid? TargetTeamId,
    IReadOnlyDictionary<string, JsonElement>? Variables = null,
    JsonElement Payload = default);
