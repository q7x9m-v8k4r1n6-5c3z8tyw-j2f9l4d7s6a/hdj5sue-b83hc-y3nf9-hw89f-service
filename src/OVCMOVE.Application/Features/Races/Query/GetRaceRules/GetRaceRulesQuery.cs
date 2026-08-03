using MediatR;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceRules;

public class GetRaceRulesQuery : IRequest<string?>
{
    public Guid RaceId { get; set; }
    public Guid TeamId { get; set; }
}