using MediatR;

namespace OVCMOVE.Application.Features.Teams.Query.SearchTeam;

public class SearchTeamQuery : IRequest<List<SearchTeamResultModel>>
{
    public string Keyword { get; set; } = string.Empty;

    public SearchTeamQuery(string keyword)
    {
        Keyword = keyword;
    }
}