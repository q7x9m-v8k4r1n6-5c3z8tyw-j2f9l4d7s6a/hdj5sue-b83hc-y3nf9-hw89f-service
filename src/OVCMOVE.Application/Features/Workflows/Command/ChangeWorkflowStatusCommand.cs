using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class ChangeWorkflowStatusCommand : AuditedRequest, IRequest<WorkflowResultModel>
{
    public Guid WorkflowId { get; init; }
    public DateTime ExpectedModifiedAt { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class ChangeWorkflowStatusCommandHandler(
    IWorkflowRepository repository,
    IFunctionCardRepository functionCardRepository,
    WorkflowDefinitionValidator validator)
    : IRequestHandler<ChangeWorkflowStatusCommand, WorkflowResultModel>
{
    public async Task<WorkflowResultModel> Handle(
        ChangeWorkflowStatusCommand request,
        CancellationToken cancellationToken)
    {
        WorkflowCommandRules.ValidateConcurrency(
            request.WorkflowId,
            request.ExpectedModifiedAt);
        var status = request.Status.Trim().ToLowerInvariant();
        if (status is not (WorkflowConstants.Status.Draft or WorkflowConstants.Status.Published or WorkflowConstants.Status.Disabled))
            throw new ApplicationValidationException("Trạng thái workflow không hợp lệ.");
        var workflow = await repository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        if (status == WorkflowConstants.Status.Published)
        {
            var card = await functionCardRepository.GetByIdAsync(workflow.CardId, cancellationToken)
                ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
            validator.Validate(
                WorkflowJson.DeserializeDefinition(workflow.DefinitionJson),
                workflow.TriggerType,
                true,
                FunctionCardInputDefinition.GetKeys(card.InputsJson));
        }

        workflow.Status = status;
        workflow.ModifiedBy = request.GetActorOrSystem();
        workflow.ModifiedAt = DateTime.UtcNow;
        if (!await repository.UpdateAsync(workflow, request.ExpectedModifiedAt, cancellationToken))
            throw new ConcurrencyConflictException("Workflow đã được người khác cập nhật. Vui lòng tải lại.");
        return (await repository.GetByIdAsync(workflow.Id, cancellationToken))!.ToResult();
    }
}
