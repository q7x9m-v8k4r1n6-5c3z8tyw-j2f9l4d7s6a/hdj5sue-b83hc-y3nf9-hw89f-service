using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.GetAllTeams;

public record GetAllTeamsQuery : IRequest<PagedResult<GetAllTeamsResultModel>>
{
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}