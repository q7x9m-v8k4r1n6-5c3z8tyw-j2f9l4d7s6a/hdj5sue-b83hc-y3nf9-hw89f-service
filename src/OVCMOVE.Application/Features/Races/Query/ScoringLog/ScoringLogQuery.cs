using MediatR;

using OVCMOVE.Application.Common; 

namespace OVCMOVE.Application.Features.Races.Query.ScoringLog;

public record ScoringLogQuery : IRequest<PagedResult<ScoringLogResultModel>> 
{
    public Guid RaceId { get; init; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20; 
}