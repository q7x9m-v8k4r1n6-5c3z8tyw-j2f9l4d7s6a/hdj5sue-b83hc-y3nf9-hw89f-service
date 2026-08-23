using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class ExecuteWorkflowCommand : AuditedRequest, IRequest<WorkflowExecutionResultModel>
{
    public Guid WorkflowId { get; init; }
    public bool IsSimulation { get; init; }
    public WorkflowExecutionInputModel Input { get; init; } = new();
}

public sealed class ExecuteWorkflowCommandHandler(
    IWorkflowRepository repository,
    IFunctionCardRepository functionCardRepository,
    WorkflowDefinitionValidator validator,
    WorkflowRuntime runtime)
    : IRequestHandler<ExecuteWorkflowCommand, WorkflowExecutionResultModel>
{
    public async Task<WorkflowExecutionResultModel> Handle(
        ExecuteWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        var workflow = await repository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        if (!request.IsSimulation && workflow.Status != WorkflowConstants.Status.Published)
            throw new ApplicationConflictException("Chỉ workflow đã xuất bản mới được phép chạy thật.");
        if (!request.IsSimulation && string.IsNullOrWhiteSpace(request.Input.EventId))
            throw new ApplicationValidationException("EventId là bắt buộc khi chạy thật để chống thực thi trùng.");

        var card = await functionCardRepository.GetByIdAsync(workflow.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        var definition = WorkflowJson.DeserializeDefinition(workflow.DefinitionJson);
        validator.Validate(
            definition,
            workflow.TriggerType,
            true,
            FunctionCardInputDefinition.GetKeys(card.InputsJson));
        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;
        var run = new WorkflowRun
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            RaceId = workflow.RaceId,
            CardKey = workflow.CardKey,
            TriggerType = workflow.TriggerType,
            EventId = string.IsNullOrWhiteSpace(request.Input.EventId) ? null : request.Input.EventId.Trim(),
            Status = "running",
            IsSimulation = request.IsSimulation,
            InputJson = JsonSerializer.Serialize(request.Input, WorkflowJson.Options),
            OutputJson = "{}",
            StartedAt = now,
            CreatedBy = actor,
            CreatedAt = now,
            ModifiedBy = actor,
            ModifiedAt = now
        };

        await repository.CreateRunAsync(run, cancellationToken);

        try
        {
            var outcome = await runtime.ExecuteAsync(
                workflow,
                definition,
                request.Input,
                request.IsSimulation,
                cancellationToken);
            var result = new WorkflowExecutionResultModel(
                run.Id,
                "succeeded",
                request.IsSimulation,
                outcome.Trace,
                outcome.Effects,
                outcome.Variables);
            run.Status = result.Status;
            run.OutputJson = JsonSerializer.Serialize(result, WorkflowJson.Options);
            run.CompletedAt = DateTime.UtcNow;
            run.ModifiedAt = run.CompletedAt.Value;
            await repository.CompleteRunAsync(run, CancellationToken.None);
            return result;
        }
        catch (OperationCanceledException)
        {
            run.Status = "canceled";
            run.Error = "Workflow execution was cancelled.";
            run.CompletedAt = DateTime.UtcNow;
            run.ModifiedAt = run.CompletedAt.Value;
            await repository.CompleteRunAsync(run, CancellationToken.None);
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            run.Status = "failed";
            run.Error = exception.Message.Length > 2000
                ? exception.Message[..2000]
                : exception.Message;
            run.CompletedAt = DateTime.UtcNow;
            run.ModifiedAt = run.CompletedAt.Value;
            await repository.CompleteRunAsync(run, CancellationToken.None);
            throw;
        }
    }
}
