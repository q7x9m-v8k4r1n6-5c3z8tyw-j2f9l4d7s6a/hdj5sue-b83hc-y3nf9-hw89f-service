using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class UpdateWorkflowCommand : AuditedRequest, IRequest<WorkflowResultModel>
{
    public Guid WorkflowId { get; init; }
    public DateTime ExpectedModifiedAt { get; init; }
    public Guid CardId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TriggerType { get; init; } = string.Empty;
    public WorkflowDefinitionModel Definition { get; init; } = new();
}

public sealed class UpdateWorkflowCommandHandler(
    IWorkflowRepository repository,
    IFunctionCardRepository functionCardRepository,
    WorkflowDefinitionValidator validator)
    : IRequestHandler<UpdateWorkflowCommand, WorkflowResultModel>
{
    public async Task<WorkflowResultModel> Handle(
        UpdateWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        WorkflowCommandRules.ValidateConcurrency(
            request.WorkflowId,
            request.ExpectedModifiedAt);
        WorkflowCommandRules.ValidateIdentity(request.CardId, request.Name);

        var workflow = await repository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        var card = await functionCardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        if (card.RaceId != workflow.RaceId)
            throw new ApplicationValidationException("Thẻ chức năng không thuộc race của workflow.");
        var triggerType = WorkflowCommandRules.TriggerForCard(card.Category);
        validator.Validate(
            request.Definition,
            triggerType,
            true,
            FunctionCardInputDefinition.GetKeys(card.InputsJson));
        var duplicate = (await repository.GetByRaceAsync(
            workflow.RaceId, card.CardKey, cancellationToken))
            .Any(item => item.Id != workflow.Id);
        if (duplicate)
            throw new ApplicationConflictException("Card đã có workflow.");

        workflow.CardId = card.Id;
        workflow.CardKey = card.CardKey;
        workflow.CardName = card.Name;
        workflow.Name = request.Name.Trim();
        workflow.Description = request.Description.Trim();
        workflow.TriggerType = triggerType;
        workflow.Status = WorkflowConstants.Status.Published;
        workflow.Version += 1;
        workflow.DefinitionJson = JsonSerializer.Serialize(request.Definition, WorkflowJson.Options);
        workflow.ModifiedBy = request.GetActorOrSystem();
        workflow.ModifiedAt = DateTime.UtcNow;

        if (!await repository.UpdateAsync(workflow, request.ExpectedModifiedAt, cancellationToken))
            throw new ConcurrencyConflictException("Workflow đã được người khác cập nhật. Vui lòng tải lại.");
        return (await repository.GetByIdAsync(workflow.Id, cancellationToken))!.ToResult();
    }
}
