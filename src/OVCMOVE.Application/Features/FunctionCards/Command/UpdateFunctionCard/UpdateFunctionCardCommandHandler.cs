using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command.UpdateFunctionCard;

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