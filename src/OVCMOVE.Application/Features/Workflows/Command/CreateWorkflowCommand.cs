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
        WorkflowCommandRules.ValidateIdentity(request.RaceId, request.CardId, request.Name);
        if (await raceRepository.GetByIdAsync(request.RaceId, cancellationToken) is null)
            throw new ApplicationNotFoundException("Không tìm thấy race.");

        var card = await functionCardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        if (card.RaceId != request.RaceId)
            throw new ApplicationValidationException("Thẻ chức năng không thuộc race đã chọn.");
        var triggerType = WorkflowCommandRules.TriggerForCard(card.Category);
        validator.Validate(
            request.Definition,
            triggerType,
            true,
            FunctionCardInputDefinition.GetKeys(card.InputsJson));
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
}
