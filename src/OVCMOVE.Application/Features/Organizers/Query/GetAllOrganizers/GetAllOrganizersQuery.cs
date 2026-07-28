using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;

public sealed class GetAllOrganizersQuery : IRequest<PagedResult<GetAllOrganizersResultModel>>
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
