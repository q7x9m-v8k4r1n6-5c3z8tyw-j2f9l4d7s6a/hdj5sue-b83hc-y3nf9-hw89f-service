using MediatR;

namespace OVCMOVE.Application.Features.Races.Query.BoothScoringLog;

public record BoothScoringLogQuery : IRequest<List<BoothScoringLogResultModel>>
{
    public Guid? RaceId { get; init; }
    public int? Limit {get; init;}
}