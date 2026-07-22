using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetAllRaces;

public class GetAllRacesQuery : IRequest<PagedResult<RaceItemResultModel>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
