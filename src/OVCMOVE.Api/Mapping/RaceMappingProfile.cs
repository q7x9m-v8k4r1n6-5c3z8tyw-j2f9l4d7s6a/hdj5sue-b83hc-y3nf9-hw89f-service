using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Api.Mapping;

public class RaceMappingProfile : Profile
{
    public RaceMappingProfile()
    {
        CreateMap<RaceContract.UpsertRaceRequest, CreateRaceCommand>();
        CreateMap<CreateRaceCommand, Race>();
        CreateMap<RaceDto.BoothInput, Booth>()
            .ForMember(dest => dest.BoothOrganizerID, opt => opt.MapFrom(src => src.OrganizerID));
        CreateMap<RaceDto.RaceTeamInputDto, RaceTeam>();
        CreateMap<Race, GetAllRacesResultModel>();
    }
}