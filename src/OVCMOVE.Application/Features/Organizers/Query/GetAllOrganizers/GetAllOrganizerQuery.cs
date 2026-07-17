using MediatR;
using OVCMOVE.Application.Common; 

namespace OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;


public class GetAllOrganizersQuery : IRequest<PagedResult<GetAllOrganizersResultModel>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20; // Cố định 20 item mỗi trang
}