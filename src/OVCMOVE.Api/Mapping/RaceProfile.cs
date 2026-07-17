using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.UpdateRace;

namespace OVCMOVE.Api.Mapping;

public class RaceProfile : Profile
{
    public RaceProfile()
    {
        CreateMap<RaceContract.UpsertRaceRequest, CreateRaceCommand>();
        CreateMap<RaceContract.UpsertRaceRequest, UpdateRaceCommand>();
    }
}
