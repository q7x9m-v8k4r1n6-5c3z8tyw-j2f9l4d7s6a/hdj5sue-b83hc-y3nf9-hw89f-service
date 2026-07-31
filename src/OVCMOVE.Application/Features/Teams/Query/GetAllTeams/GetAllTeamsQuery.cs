using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.GetAllTeams;

/// <summary>
/// Query lấy danh sách Teams hỗ trợ phân trang và tìm kiếm theo từ khóa.
/// </summary>
public record GetAllTeamsQuery : IRequest<PagedResult<GetAllTeamsResultModel>>
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}