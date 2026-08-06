using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceRules;

public class GetAdminRaceRulesQueryHandler : IRequestHandler<GetAdminRaceRulesQuery, string?>
{
    private readonly IRaceRepository _raceRepository;

    public GetAdminRaceRulesQueryHandler(IRaceRepository raceRepository)
    {
        _raceRepository = raceRepository;
    }

    public Task<string?> Handle(GetAdminRaceRulesQuery request, CancellationToken cancellationToken)
    {
        return _raceRepository.GetRulesAsync(request.RaceId, cancellationToken);
    }
}