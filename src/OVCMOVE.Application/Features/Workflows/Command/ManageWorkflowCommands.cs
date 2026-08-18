using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class CreateWorkflowCommand : AuditedRequest, IRequest<WorkflowResultModel>
{
    public Guid RaceId { get; init; }
    public Guid CardId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TriggerType { get; init; } = string.Empty;
    public WorkflowDefinitionModel Definition { get; init; } = new();
}

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

public sealed class ChangeWorkflowStatusCommand : AuditedRequest, IRequest<WorkflowResultModel>
{
    public Guid WorkflowId { get; init; }
    public DateTime ExpectedModifiedAt { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class DeleteWorkflowCommand : AuditedRequest, IRequest<bool>
{
    public Guid WorkflowId { get; init; }
}

public sealed class CreateWorkflowCommandHandler(
    IWorkflowRepository workflowRepository,
    IFunctionCardRepository functionCardRepository,
    IRaceRepository raceRepository,
    WorkflowDefinitionValidator validator)
    : IRequestHandler<CreateWorkflowCommand, WorkflowResultModel>
{
    public async Task<WorkflowResultModel> Handle(
        CreateWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(request.RaceId, request.CardId, request.Name);
        if (await raceRepository.GetByIdAsync(request.RaceId, cancellationToken) is null)
            throw new ApplicationNotFoundException("Không tìm thấy race.");

        var card = await functionCardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        if (card.RaceId != request.RaceId)
            throw new ApplicationValidationException("Thẻ chức năng không thuộc race đã chọn.");
        var triggerType = TriggerForCard(card.Category);
        validator.Validate(request.Definition, triggerType, true);
        var duplicate = (await workflowRepository.GetByRaceAsync(
            request.RaceId, card.CardKey, cancellationToken))
            .Any();
        if (duplicate)
            throw new ApplicationConflictException("Card đã có workflow.");

        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            CardId = card.Id,
            RaceId = request.RaceId,
            CardKey = card.CardKey,
            CardName = card.Name,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            TriggerType = triggerType,
            Status = WorkflowConstants.Status.Published,
            Version = 1,
            DefinitionJson = JsonSerializer.Serialize(request.Definition, WorkflowJson.Options),
            CreatedBy = actor,
            CreatedAt = now,
            ModifiedBy = actor,
            ModifiedAt = now
        };
        await workflowRepository.CreateAsync(workflow, cancellationToken);
        return (await workflowRepository.GetByIdAsync(workflow.Id, cancellationToken))!.ToResult();
    }

    private static void ValidateIdentity(Guid raceId, Guid cardId, string name)
    {
        if (raceId == Guid.Empty) throw new ApplicationValidationException("RaceId là bắt buộc.");
        if (cardId == Guid.Empty) throw new ApplicationValidationException("CardId là bắt buộc.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255)
            throw new ApplicationValidationException("Tên workflow phải có từ 1 đến 255 ký tự.");
    }

    internal static void ValidateIdentity(UpdateWorkflowCommand request) =>
        ValidateIdentity(Guid.NewGuid(), request.CardId, request.Name);

    public static string TriggerForCard(string category) =>
        string.Equals(category, FunctionCardConstants.Category.Defense, StringComparison.OrdinalIgnoreCase)
            ? WorkflowConstants.Trigger.Attacked
            : WorkflowConstants.Trigger.Activated;
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
        if (request.WorkflowId == Guid.Empty || request.ExpectedModifiedAt == default)
            throw new ApplicationValidationException("WorkflowId và expectedModifiedAt là bắt buộc.");
        CreateWorkflowCommandHandler.ValidateIdentity(request);

        var workflow = await repository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        var card = await functionCardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        if (card.RaceId != workflow.RaceId)
            throw new ApplicationValidationException("Thẻ chức năng không thuộc race của workflow.");
        var triggerType = CreateWorkflowCommandHandler.TriggerForCard(card.Category);
        validator.Validate(request.Definition, triggerType, true);
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

public sealed class ChangeWorkflowStatusCommandHandler(
    IWorkflowRepository repository,
    WorkflowDefinitionValidator validator)
    : IRequestHandler<ChangeWorkflowStatusCommand, WorkflowResultModel>
{
    public async Task<WorkflowResultModel> Handle(
        ChangeWorkflowStatusCommand request,
        CancellationToken cancellationToken)
    {
        var status = request.Status.Trim().ToLowerInvariant();
        if (status is not (WorkflowConstants.Status.Draft or WorkflowConstants.Status.Published or WorkflowConstants.Status.Disabled))
            throw new ApplicationValidationException("Trạng thái workflow không hợp lệ.");
        var workflow = await repository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        if (status == WorkflowConstants.Status.Published)
            validator.Validate(WorkflowJson.DeserializeDefinition(workflow.DefinitionJson), workflow.TriggerType, true);

        workflow.Status = status;
        workflow.ModifiedBy = request.GetActorOrSystem();
        workflow.ModifiedAt = DateTime.UtcNow;
        if (!await repository.UpdateAsync(workflow, request.ExpectedModifiedAt, cancellationToken))
            throw new ConcurrencyConflictException("Workflow đã được người khác cập nhật. Vui lòng tải lại.");
        return (await repository.GetByIdAsync(workflow.Id, cancellationToken))!.ToResult();
    }
}

public sealed class DeleteWorkflowCommandHandler(IWorkflowRepository repository)
    : IRequestHandler<DeleteWorkflowCommand, bool>
{
    public async Task<bool> Handle(DeleteWorkflowCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.SoftDeleteAsync(
            request.WorkflowId, request.GetActorOrSystem(), DateTime.UtcNow, cancellationToken))
            throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        return true;
    }
}
