using System.Text.Json;
using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.FunctionCards.Command;

public abstract class FunctionCardMutationRequest : AuditedRequest
{
    public string CardKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? BackgroundUrl { get; init; }
    public JsonElement Inputs { get; init; }
}

public sealed class CreateFunctionCardCommand : FunctionCardMutationRequest, IRequest<FunctionCardResultModel>
{
    public Guid RaceId { get; init; }
}

public sealed class UpdateFunctionCardCommand : FunctionCardMutationRequest, IRequest<FunctionCardResultModel>
{
    public Guid CardId { get; init; }
    public DateTime ExpectedModifiedAt { get; init; }
}

public sealed class AssignFunctionCardTeamCommand : AuditedRequest, IRequest<FunctionCardResultModel>
{
    public Guid CardId { get; init; }
    public Guid? TeamId { get; init; }
    public DateTime ExpectedModifiedAt { get; init; }
}

public sealed class DeleteFunctionCardCommand : AuditedRequest, IRequest<bool>
{
    public Guid CardId { get; init; }
}

public sealed class CreateFunctionCardCommandHandler(
    IFunctionCardRepository repository,
    IRaceRepository raceRepository)
    : IRequestHandler<CreateFunctionCardCommand, FunctionCardResultModel>
{
    public async Task<FunctionCardResultModel> Handle(
        CreateFunctionCardCommand request,
        CancellationToken cancellationToken)
    {
        FunctionCardValidator.Validate(request.CardKey, request.Name, request.Description,
            request.Category, request.BackgroundUrl, request.Inputs);
        if (request.RaceId == Guid.Empty ||
            await raceRepository.GetByIdAsync(request.RaceId, cancellationToken) is null)
            throw new ApplicationNotFoundException("Không tìm thấy race.");

        var cardKey = request.CardKey.Trim();
        if (await repository.GetByKeyAsync(request.RaceId, cardKey, cancellationToken) is not null)
            throw new ApplicationConflictException("CardKey đã tồn tại trong race này.");

        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;
        var card = new FunctionCard
        {
            Id = Guid.NewGuid(),
            RaceId = request.RaceId,
            TeamId = null,
            CardKey = cardKey,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim().ToLowerInvariant(),
            BackgroundUrl = string.IsNullOrWhiteSpace(request.BackgroundUrl) ? null : request.BackgroundUrl.Trim(),
            InputsJson = request.Inputs.GetRawText(),
            CreatedBy = actor,
            CreatedAt = now,
            ModifiedBy = actor,
            ModifiedAt = now
        };
        await repository.CreateAsync(card, cancellationToken);
        return (await repository.GetDetailAsync(card.Id, cancellationToken))!.ToResult();
    }
}

public sealed class UpdateFunctionCardCommandHandler(IFunctionCardRepository repository)
    : IRequestHandler<UpdateFunctionCardCommand, FunctionCardResultModel>
{
    public async Task<FunctionCardResultModel> Handle(
        UpdateFunctionCardCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CardId == Guid.Empty || request.ExpectedModifiedAt == default)
            throw new ApplicationValidationException("CardId và expectedModifiedAt là bắt buộc.");
        FunctionCardValidator.Validate(request.CardKey, request.Name, request.Description,
            request.Category, request.BackgroundUrl, request.Inputs);

        var card = await repository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        var duplicate = await repository.GetByKeyAsync(card.RaceId, request.CardKey.Trim(), cancellationToken);
        if (duplicate is not null && duplicate.Id != card.Id)
            throw new ApplicationConflictException("CardKey đã tồn tại trong race này.");

        card.CardKey = request.CardKey.Trim();
        card.Name = request.Name.Trim();
        card.Description = request.Description.Trim();
        card.Category = request.Category.Trim().ToLowerInvariant();
        card.BackgroundUrl = string.IsNullOrWhiteSpace(request.BackgroundUrl) ? null : request.BackgroundUrl.Trim();
        card.InputsJson = request.Inputs.GetRawText();
        card.ModifiedBy = request.GetActorOrSystem();
        card.ModifiedAt = DateTime.UtcNow;
        if (!await repository.UpdateAsync(card, request.ExpectedModifiedAt, cancellationToken))
            throw new ConcurrencyConflictException("Thẻ đã được người khác cập nhật. Vui lòng tải lại.");
        return (await repository.GetDetailAsync(card.Id, cancellationToken))!.ToResult();
    }
}

public sealed class AssignFunctionCardTeamCommandHandler(
    IFunctionCardRepository repository,
    IRaceRepository raceRepository)
    : IRequestHandler<AssignFunctionCardTeamCommand, FunctionCardResultModel>
{
    public async Task<FunctionCardResultModel> Handle(
        AssignFunctionCardTeamCommand request,
        CancellationToken cancellationToken)
    {
        if (request.CardId == Guid.Empty || request.ExpectedModifiedAt == default)
            throw new ApplicationValidationException("CardId và expectedModifiedAt là bắt buộc.");
        var card = await repository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        if (request.TeamId.HasValue &&
            !await raceRepository.IsTeamInRaceAsync(card.RaceId, request.TeamId.Value, cancellationToken))
            throw new ApplicationValidationException("Team được chọn không thuộc race của thẻ.");

        if (!await repository.AssignTeamAsync(
            card.Id, request.TeamId, request.GetActorOrSystem(), request.ExpectedModifiedAt,
            DateTime.UtcNow, cancellationToken))
            throw new ConcurrencyConflictException("Thẻ đã được người khác cập nhật. Vui lòng tải lại.");
        return (await repository.GetDetailAsync(card.Id, cancellationToken))!.ToResult();
    }
}

public sealed class DeleteFunctionCardCommandHandler(IFunctionCardRepository repository)
    : IRequestHandler<DeleteFunctionCardCommand, bool>
{
    public async Task<bool> Handle(DeleteFunctionCardCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.SoftDeleteAsync(
            request.CardId, request.GetActorOrSystem(), DateTime.UtcNow, cancellationToken))
            throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
        return true;
    }
}
