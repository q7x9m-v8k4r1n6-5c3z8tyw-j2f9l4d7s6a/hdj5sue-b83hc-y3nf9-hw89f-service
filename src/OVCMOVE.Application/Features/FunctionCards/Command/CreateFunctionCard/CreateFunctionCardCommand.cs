using MediatR;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command.CreateFunctionCard;

public sealed class CreateFunctionCardCommand : FunctionCardMutationCommandBase, IRequest<FunctionCardResultModel>
{
    public Guid RaceId { get; init; }
}