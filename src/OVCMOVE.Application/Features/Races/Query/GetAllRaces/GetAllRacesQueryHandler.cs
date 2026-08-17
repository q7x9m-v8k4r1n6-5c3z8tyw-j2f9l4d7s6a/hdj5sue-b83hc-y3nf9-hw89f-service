using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetAllRaces;

public class GetAllRacesQueryHandler :
    IRequestHandler<GetAllRacesQuery, PagedResult<RaceItemResultModel>>
{
    private readonly IRaceRepository _raceRepository;

    public GetAllRacesQueryHandler(IRaceRepository raceRepository)
    {
        _raceRepository = raceRepository;
    }

    /// <summary>Returns one database-paged list of races.</summary>
    public async Task<PagedResult<RaceItemResultModel>> Handle(GetAllRacesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (page, pageSize) = Pagination.Normalize(
            request.Page,
            request.PageSize);
        var (items, totalItems) = await _raceRepository.GetPageAsync(
            page,
            pageSize,
            request.TeamId,
            request.OrganizerId,
            request.RuntimeStatusesOnly,
            cancellationToken);

        return new PagedResult<RaceItemResultModel>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = items
        };
    }
}
