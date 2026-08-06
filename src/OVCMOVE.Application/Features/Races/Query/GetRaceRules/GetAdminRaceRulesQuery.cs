using MediatR;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceRules;

public class GetAdminRaceRulesQuery : IRequest<string?>
{
    public Guid RaceId { get; set; }
}