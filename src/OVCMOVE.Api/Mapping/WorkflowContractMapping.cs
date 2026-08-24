using System.Text.Json;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Workflows.Command;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Api.Mapping;

public static class WorkflowContractMapping
{
    public static CreateWorkflowCommand ToCommand(
        this WorkflowContract.CreateWorkflowRequest request,
        Guid raceId) => new()
        {
            RaceId = raceId,
            CardId = request.CardId,
            Name = request.Name,
            Description = request.Description,
            TriggerType = request.TriggerType,
            Definition = ParseDefinition(request.Definition)
        };

    public static UpdateWorkflowCommand ToCommand(
        this WorkflowContract.UpdateWorkflowRequest request,
        Guid workflowId) => new()
        {
            WorkflowId = workflowId,
            ExpectedModifiedAt = request.ExpectedModifiedAt,
            CardId = request.CardId,
            Name = request.Name,
            Description = request.Description,
            TriggerType = request.TriggerType,
            Definition = ParseDefinition(request.Definition)
        };

    public static ExecuteWorkflowCommand ToCommand(
        this WorkflowContract.ExecuteWorkflowRequest request,
        Guid workflowId) => new()
        {
            WorkflowId = workflowId,
            IsSimulation = request.IsSimulation,
            Input = new WorkflowExecutionInputModel
            {
                EventId = request.EventId,
                ActorTeamId = request.ActorTeamId,
                TargetTeamId = request.TargetTeamId,
                Variables = request.Variables ??
                    new Dictionary<string, JsonElement>(),
                Payload = request.Payload
            }
        };

    private static WorkflowDefinitionModel ParseDefinition(JsonElement definition)
    {
        try
        {
            return JsonSerializer.Deserialize<WorkflowDefinitionModel>(
                definition.GetRawText(), WorkflowJson.Options)
                ?? throw new ApplicationValidationException("Workflow definition không được để trống.");
        }
        catch (JsonException exception)
        {
            throw new ApplicationValidationException(
                $"Workflow definition không hợp lệ: {exception.Message}");
        }
        catch (InvalidOperationException)
        {
            throw new ApplicationValidationException("Workflow definition không được để trống.");
        }
    }
}
