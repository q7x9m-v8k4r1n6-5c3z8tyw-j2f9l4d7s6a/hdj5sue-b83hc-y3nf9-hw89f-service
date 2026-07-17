using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.GetAllTeams;

public class GetAllTeamsQuery : IRequest<PagedResult<GetAllTeamsResultModel>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20; // Cố định 20 item mỗi trang 
}