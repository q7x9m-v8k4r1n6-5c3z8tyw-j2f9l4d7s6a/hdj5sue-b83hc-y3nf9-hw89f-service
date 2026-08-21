using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Query;

public sealed record GetFunctionCardsQuery(Guid RaceId)
    : IRequest<IReadOnlyCollection<FunctionCardResultModel>>;

public sealed record GetFunctionCardDetailQuery(Guid CardId)
    : IRequest<FunctionCardResultModel>;

public sealed class GetFunctionCardsQueryHandler(
    IFunctionCardRepository repository,
    IRaceRepository raceRepository)
    : IRequestHandler<GetFunctionCardsQuery, IReadOnlyCollection<FunctionCardResultModel>>
{
    public async Task<IReadOnlyCollection<FunctionCardResultModel>> Handle(
        GetFunctionCardsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId == Guid.Empty)
            throw new ApplicationValidationException("RaceId là bắt buộc.");
        if (await raceRepository.GetByIdAsync(request.RaceId, cancellationToken) is null)
            throw new ApplicationNotFoundException("Không tìm thấy race.");
        return (await repository.GetByRaceAsync(request.RaceId, cancellationToken))
            .Select(item => item.ToResult())
            .ToArray();
    }
}

public sealed class GetFunctionCardDetailQueryHandler(IFunctionCardRepository repository)
    : IRequestHandler<GetFunctionCardDetailQuery, FunctionCardResultModel>
{
    public async Task<FunctionCardResultModel> Handle(
        GetFunctionCardDetailQuery request,
        CancellationToken cancellationToken) =>
        (await repository.GetDetailAsync(request.CardId, cancellationToken))?.ToResult()
        ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ chức năng.");
}
