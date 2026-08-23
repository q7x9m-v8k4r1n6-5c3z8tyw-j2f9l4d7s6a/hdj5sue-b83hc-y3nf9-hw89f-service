using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Abstractions.Repositories;


namespace OVCMOVE.Application.Features.FunctionCards.Query.GetRaceCardsForAdmin;

public sealed class GetRaceCardsForAdminQueryHandler(
    IFunctionCardRepository repository,
    IRaceRepository raceRepository)
    : IRequestHandler<GetRaceCardsForAdminQuery, IReadOnlyCollection<FunctionCardResultModel>>
{
    public async Task<IReadOnlyCollection<FunctionCardResultModel>> Handle(
        GetRaceCardsForAdminQuery request,
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