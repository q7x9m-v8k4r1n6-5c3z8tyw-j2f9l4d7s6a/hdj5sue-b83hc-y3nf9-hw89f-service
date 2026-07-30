using MediatR;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races.Query.ScoringLog;

public class ScoringLogQueryHandler : 
    IRequestHandler<ScoringLogQuery, PagedResult<ScoringLogResultModel>>
{
    private readonly IRaceRepository _raceRepository;
    public ScoringLogQueryHandler(
        IRaceRepository raceRepository)
    {
        _raceRepository = raceRepository;
    }
    
    public async Task<PagedResult<ScoringLogResultModel>> Handle(
        ScoringLogQuery request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (page, pageSize) = Pagination.Normalize(
            request.Page, 
            request.PageSize);
            
        var (items, totalItems) = await _raceRepository.GetScoringLogPageByRaceIdAsync(
            request.RaceId, 
            page, 
            pageSize, 
            cancellationToken);
            
        return new PagedResult<ScoringLogResultModel>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = items
        };
    }
}