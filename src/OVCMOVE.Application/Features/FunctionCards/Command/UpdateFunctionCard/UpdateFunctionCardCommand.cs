using MediatR;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command.UpdateFunctionCard;

public sealed class UpdateFunctionCardCommand : FunctionCardMutationCommandBase, IRequest<FunctionCardResultModel>
{
    public Guid CardId { get; init; }
    public DateTime ExpectedModifiedAt { get; init; }
}