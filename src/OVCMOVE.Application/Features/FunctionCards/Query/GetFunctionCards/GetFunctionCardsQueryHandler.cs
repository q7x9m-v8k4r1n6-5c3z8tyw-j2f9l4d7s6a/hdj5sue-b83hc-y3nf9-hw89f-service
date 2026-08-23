using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Abstractions.Repositories;


namespace OVCMOVE.Application.Features.FunctionCards.Query.GetFunctionCards;

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