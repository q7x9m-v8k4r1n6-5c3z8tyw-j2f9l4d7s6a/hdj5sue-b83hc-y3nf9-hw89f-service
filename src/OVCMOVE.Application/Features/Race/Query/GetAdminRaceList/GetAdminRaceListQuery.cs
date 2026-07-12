using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Query.GetAdminRaceList;

public class GetAdminRaceListQuery : IRequest<RaceResultModel.AdminRaceListResultModel>
{
}
