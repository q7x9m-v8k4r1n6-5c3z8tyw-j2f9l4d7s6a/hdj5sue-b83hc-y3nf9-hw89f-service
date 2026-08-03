using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceRules;

public class GetRaceRulesQueryHandler : IRequestHandler<GetRaceRulesQuery, string?>
{
    private readonly IRaceRepository _raceRepository;

    public GetRaceRulesQueryHandler(IRaceRepository raceRepository)
    {
        _raceRepository = raceRepository;
    }

    public async Task<string?> Handle(GetRaceRulesQuery request, CancellationToken cancellationToken)
    {
        var isTeamInRace = await _raceRepository.IsTeamInRaceAsync(
            request.RaceId, request.TeamId, cancellationToken);
        if (!isTeamInRace) return null;

        return await _raceRepository.GetRulesAsync(request.RaceId, cancellationToken);
    }
}