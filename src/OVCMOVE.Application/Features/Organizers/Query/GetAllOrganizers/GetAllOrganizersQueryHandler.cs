using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;

public class GetAllOrganizersQueryHandler :
    IRequestHandler<GetAllOrganizersQuery, PagedResult<GetAllOrganizersResultModel>>
{
    private readonly IOrganizerRepository _organizerRepository;

    public GetAllOrganizersQueryHandler(
        IOrganizerRepository organizerRepository)
    {
        _organizerRepository = organizerRepository;
    }

    /// <summary>Returns one normalized page of organizer accounts.</summary>
    public async Task<PagedResult<GetAllOrganizersResultModel>> Handle(
        GetAllOrganizersQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Chuẩn hóa số trang (Tránh số âm hoặc số quá lớn)
        var (page, pageSize) = Pagination.Normalize(
            request.Page,
            request.PageSize);

        // 2. Phân trang trực tiếp từ SQL/DB (Cực kỳ nhanh)
        var (organizers, totalItems) = await _organizerRepository.GetPageAsync(
            request.Search,
            page,
            pageSize,
            cancellationToken);

        // 3. Trả về kết quả PagedResult
        return new PagedResult<GetAllOrganizersResultModel>
        {
            Items = organizers,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }
}