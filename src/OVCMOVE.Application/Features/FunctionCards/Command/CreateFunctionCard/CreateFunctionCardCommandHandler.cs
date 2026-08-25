using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.FunctionCards.Command.CreateFunctionCard;

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