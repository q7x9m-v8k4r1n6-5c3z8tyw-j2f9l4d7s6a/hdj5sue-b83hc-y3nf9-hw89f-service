using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.FunctionCards.Command.CreateFunctionCard;
using OVCMOVE.Application.Features.FunctionCards.Command.UpdateFunctionCard;
using OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardDetail;
using OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardInventory;

namespace OVCMOVE.Api.Mapping;

public static class FunctionCardContractMapping
{
    public static CreateFunctionCardCommand ToCommand(
        this FunctionCardContract.MutationRequest request,
        Guid raceId) => new()
        {
            RaceId = raceId,
            CardKey = request.CardKey,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            BackgroundUrl = request.BackgroundUrl,
            Inputs = request.Inputs
        };

    public static UpdateFunctionCardCommand ToCommand(
        this FunctionCardContract.UpdateRequest request,
        Guid cardId) => new()
        {
            CardId = cardId,
            ExpectedModifiedAt = request.ExpectedModifiedAt,
            CardKey = request.CardKey,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            BackgroundUrl = request.BackgroundUrl,
            Inputs = request.Inputs
        };

    public static FunctionCardContract.CardsResponse ToResponse(this TeamCardInventoryItemModel model) => new()
    {
        CardId = model.CardId,
        CardUrl = model.CardUrl,
        CardName = model.CardName,
        CardType = model.CardType,
        CardStatus = model.CardStatus
    };

    public static FunctionCardContract.CardInfoResponse ToResponse(this TeamCardDetailModel model) => new()
    {
        CardInfo = model.CardInfo
    };
}
