using MediatR;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceRules;

public class GetRaceRulesQuery : IRequest<GetRaceRulesResultModel>
{
    public Guid RaceId { get; set; }
    public Guid TeamId { get; set; }
}

public sealed record GetRaceRulesResultModel(
    bool IsTeamInRace,
    string Rules);
