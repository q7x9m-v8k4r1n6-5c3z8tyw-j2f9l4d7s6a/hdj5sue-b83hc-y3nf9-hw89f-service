using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceRules;

public class GetRaceRulesQueryHandler : IRequestHandler<GetRaceRulesQuery, GetRaceRulesResultModel>
{
    private readonly IRaceRepository _raceRepository;

    public GetRaceRulesQueryHandler(IRaceRepository raceRepository)
    {
        _raceRepository = raceRepository;
    }

    public async Task<GetRaceRulesResultModel> Handle(
        GetRaceRulesQuery request,
        CancellationToken cancellationToken)
    {
        var isTeamInRace = await _raceRepository.IsTeamInRaceAsync(
            request.RaceId, request.TeamId, cancellationToken);
        if (!isTeamInRace)
        {
            return new GetRaceRulesResultModel(false, string.Empty);
        }

        var rules = await _raceRepository.GetRulesAsync(
            request.RaceId,
            cancellationToken);

        return new GetRaceRulesResultModel(true, rules ?? string.Empty);
    }
}
