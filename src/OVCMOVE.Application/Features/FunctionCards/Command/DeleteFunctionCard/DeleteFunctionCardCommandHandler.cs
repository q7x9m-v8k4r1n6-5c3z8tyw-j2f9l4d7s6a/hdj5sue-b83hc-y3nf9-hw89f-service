using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command.DeleteFunctionCard;

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