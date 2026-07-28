using MediatR;

namespace OVCMOVE.Application.Features.Teams.Query.SearchTeam;

public sealed record SearchTeamQuery(string Keyword)
    : IRequest<IReadOnlyCollection<SearchTeamResultModel>>;
