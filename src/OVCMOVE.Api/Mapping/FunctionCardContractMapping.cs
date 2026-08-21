using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.FunctionCards.Command;

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
}
