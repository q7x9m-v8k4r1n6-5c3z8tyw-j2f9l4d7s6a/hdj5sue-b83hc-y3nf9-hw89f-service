using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetFunctionCardDetail;

public sealed class GetFunctionCardDetailQueryHandler(IFunctionCardRepository repository)
    : IRequestHandler<GetFunctionCardDetailQuery, FunctionCardResultModel>
{
    public async Task<FunctionCardResultModel> Handle(
        GetFunctionCardDetailQuery request,
        CancellationToken cancellationToken) =>
        (await repository.GetDetailAsync(request.CardId, cancellationToken))?.ToResult()
        ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
}