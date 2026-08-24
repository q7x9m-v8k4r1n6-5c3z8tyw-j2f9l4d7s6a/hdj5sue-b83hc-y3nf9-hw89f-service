using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions;
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
    WorkflowRuntime runtime,
    IUnitOfWork unitOfWork,
    WorkflowRetryPolicy retryPolicy,
    WorkflowRealtimeBuffer realtimeBuffer,
    WorkflowRealtimePublisher realtimePublisher)
    : IRequestHandler<ExecuteWorkflowCommand, WorkflowExecutionResultModel>
{
    public async Task<WorkflowExecutionResultModel> Handle(
        ExecuteWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        WorkflowCommandRules.ValidateWorkflowId(request.WorkflowId);
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
            Status = WorkflowConstants.RunStatus.Running,
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

        if (request.IsSimulation || unitOfWork.HasActiveTransaction)
        {
            return await ExecuteWithoutOwnedTransactionAsync(
                workflow,
                definition,
                request,
                run,
                cancellationToken);
        }

        try
        {
            var result = await retryPolicy.ExecuteAsync(
                async (_, attemptCancellationToken) =>
                {
                    realtimeBuffer.Reset();
                    await unitOfWork.BeginAsync(attemptCancellationToken);
                    try
                    {
                        var attemptResult = await ExecuteAndCompleteAsync(
                            workflow,
                            definition,
                            request,
                            run,
                            attemptCancellationToken);
                        await unitOfWork.CommitAsync(attemptCancellationToken);
                        return attemptResult;
                    }
                    catch
                    {
                        await unitOfWork.RollbackAsync(CancellationToken.None);
                        realtimeBuffer.Reset();
                        throw;
                    }
                },
                cancellationToken);

            var realtimeSynced = await realtimePublisher.PublishAsync(
                realtimeBuffer.Snapshot(),
                CancellationToken.None);
            realtimeBuffer.Reset();
            return realtimeSynced
                ? result
                : result with
                {
                    RealtimeSynced = false,
                    Message = "Thao tác đã được ghi nhận, nhưng dữ liệu trực tiếp chưa thể đồng bộ. Vui lòng tải lại để xem trạng thái mới nhất."
                };
        }
        catch (OperationCanceledException)
        {
            realtimeBuffer.Reset();
            await CompleteFailedRunAsync(
                run,
                WorkflowConstants.RunStatus.Canceled,
                "Workflow execution was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            realtimeBuffer.Reset();
            await CompleteFailedRunAsync(
                run,
                WorkflowConstants.RunStatus.Failed,
                exception.Message);
            throw;
        }
    }

    private async Task<WorkflowExecutionResultModel> ExecuteWithoutOwnedTransactionAsync(
        Workflow workflow,
        WorkflowDefinitionModel definition,
        ExecuteWorkflowCommand request,
        WorkflowRun run,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAndCompleteAsync(
                workflow,
                definition,
                request,
                run,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await CompleteFailedRunAsync(
                run,
                WorkflowConstants.RunStatus.Canceled,
                "Workflow execution was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            await CompleteFailedRunAsync(
                run,
                WorkflowConstants.RunStatus.Failed,
                exception.Message);
            throw;
        }
    }

    private async Task<WorkflowExecutionResultModel> ExecuteAndCompleteAsync(
        Workflow workflow,
        WorkflowDefinitionModel definition,
        ExecuteWorkflowCommand request,
        WorkflowRun run,
        CancellationToken cancellationToken)
    {
        var outcome = await runtime.ExecuteAsync(
            workflow,
            definition,
            request.Input,
            request.IsSimulation,
            cancellationToken);
        var result = new WorkflowExecutionResultModel(
            run.Id,
            WorkflowConstants.RunStatus.Succeeded,
            request.IsSimulation,
            outcome.Trace,
            outcome.Effects,
            outcome.Variables);
        run.Status = result.Status;
        run.OutputJson = JsonSerializer.Serialize(result, WorkflowJson.Options);
        run.Error = null;
        run.CompletedAt = DateTime.UtcNow;
        run.ModifiedAt = run.CompletedAt.Value;
        await repository.CompleteRunAsync(run, cancellationToken);
        return result;
    }

    private async Task CompleteFailedRunAsync(
        WorkflowRun run,
        string status,
        string error)
    {
        run.Status = status;
        run.Error = error.Length > 2000 ? error[..2000] : error;
        run.CompletedAt = DateTime.UtcNow;
        run.ModifiedAt = run.CompletedAt.Value;
        await repository.CompleteRunAsync(run, CancellationToken.None);
    }
}
