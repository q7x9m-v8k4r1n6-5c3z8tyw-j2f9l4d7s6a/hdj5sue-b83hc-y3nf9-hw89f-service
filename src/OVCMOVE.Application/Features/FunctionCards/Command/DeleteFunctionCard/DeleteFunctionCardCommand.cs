using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command.DeleteFunctionCard;

public sealed class DeleteFunctionCardCommand : AuditedRequest, IRequest<bool>
{
    public Guid CardId { get; init; }
}


