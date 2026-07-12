using MediatR;
using OVCMOVE.Application.DTOs.RequestModels;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Command.CreateRace;

public class CreateRaceCommand : RaceWriteModel, IRequest<RaceResultModel.AdminRaceDetailResultModel>
{
}
