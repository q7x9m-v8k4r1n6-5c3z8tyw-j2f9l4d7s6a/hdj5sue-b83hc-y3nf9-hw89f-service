using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Query.GetAdminRaceDetail;

public class GetAdminRaceDetailQuery : IRequest<RaceResultModel.AdminRaceDetailResultModel?>
{
    public int RaceId { get; init; }
}
