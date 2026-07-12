using MediatR;
using OVCMOVE.Application.DTOs.RequestModels;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Race.Command.UpdateRace;

public class UpdateRaceCommand : RaceWriteModel, IRequest<RaceResultModel.AdminRaceDetailResultModel?>
{
    public int RaceId { get; set; }
}
