using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class CardWorkflowDispatcher(
    IWorkflowRepository repository,
    ISender sender) : ICardWorkflowDispatcher
{
    public async Task<WorkflowExecutionResultModel?> DispatchAsync(
        CardWorkflowEvent cardEvent,
        CancellationToken cancellationToken = default)
    {
        if (cardEvent.RaceId == Guid.Empty || string.IsNullOrWhiteSpace(cardEvent.CardKey))
            throw new ApplicationValidationException("RaceId và CardKey là bắt buộc.");
        if (cardEvent.TriggerType is not (WorkflowConstants.Trigger.Activated or WorkflowConstants.Trigger.Attacked))
            throw new ApplicationValidationException("Trigger card không hợp lệ.");
        if (string.IsNullOrWhiteSpace(cardEvent.EventId))
            throw new ApplicationValidationException("EventId là bắt buộc.");

        var workflow = (await repository.GetByRaceAsync(
            cardEvent.RaceId,
            cardEvent.CardKey.Trim(),
            cancellationToken)).SingleOrDefault(item =>
                item.TriggerType == cardEvent.TriggerType &&
                item.Status == WorkflowConstants.Status.Published);
        if (workflow is null) return null;

        return await sender.Send(new ExecuteWorkflowCommand
        {
            WorkflowId = workflow.Id,
            IsSimulation = false,
            Input = new WorkflowExecutionInputModel
            {
                EventId = cardEvent.EventId.Trim(),
                ActorTeamId = cardEvent.ActorTeamId,
                TargetTeamId = cardEvent.TargetTeamId,
                Variables = cardEvent.Variables
                    ?? new Dictionary<string, System.Text.Json.JsonElement>(),
                Payload = cardEvent.Payload
            }
        }, cancellationToken);
    }
}
