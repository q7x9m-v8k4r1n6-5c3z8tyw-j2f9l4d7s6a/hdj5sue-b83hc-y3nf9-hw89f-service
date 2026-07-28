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
    public async Task<PagedResult<GetAllOrganizersResultModel>> Handle(GetAllOrganizersQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (page, pageSize) = Pagination.Normalize(
            request.Page,
            request.PageSize);
        var (organizers, totalItems) = await _organizerRepository.GetPageAsync(
            request.Search,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<GetAllOrganizersResultModel>
        {
            Items = organizers,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

}
