using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetCardDetailForAdmin;

public sealed class GetCardDetailForAdminQueryHandler(IFunctionCardRepository repository)
    : IRequestHandler<GetCardDetailForAdminQuery, FunctionCardResultModel>
{
    public async Task<FunctionCardResultModel> Handle(
        GetCardDetailForAdminQuery request,
        CancellationToken cancellationToken) =>
        (await repository.GetDetailAsync(request.CardId, cancellationToken))?.ToResult()
        ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
}