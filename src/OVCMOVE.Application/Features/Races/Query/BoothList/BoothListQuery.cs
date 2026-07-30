using MediatR;

namespace OVCMOVE.Application.Features.Races.Query.BoothList;

public record BoothListQuery : IRequest<List<BoothListResultModel>>
{
    public Guid? RaceId { get; set; }
}