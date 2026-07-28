using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.GetAllTeams;

public class GetAllTeamsQuery : IRequest<PagedResult<GetAllTeamsResultModel>>
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
