using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.RequestModels;
using OVCMOVE.Application.Features.Race.Command.CreateRace;
using OVCMOVE.Application.Features.Race.Command.UpdateRace;
using static OVCMOVE.Api.Contracts.RaceContract;

namespace OVCMOVE.Api.Mapping;

public class RaceProfile : Profile
{
    public RaceProfile()
    {
        CreateMap<RaceStationRequestModel, RaceStationWriteModel>();
        CreateMap<RaceNameMissionRequestModel, RaceNameMissionWriteModel>();
        CreateMap<UpsertRaceRequestModel, CreateRaceCommand>();
        CreateMap<UpsertRaceRequestModel, UpdateRaceCommand>();
    }
}
