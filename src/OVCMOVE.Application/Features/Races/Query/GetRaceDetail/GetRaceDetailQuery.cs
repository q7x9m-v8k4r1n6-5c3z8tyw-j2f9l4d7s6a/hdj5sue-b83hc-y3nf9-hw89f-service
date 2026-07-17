using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceDetail;

public class GetRaceDetailQuery : IRequest<RaceDetailResultModel?>
{
    public Guid RaceId { get; init; }
}
