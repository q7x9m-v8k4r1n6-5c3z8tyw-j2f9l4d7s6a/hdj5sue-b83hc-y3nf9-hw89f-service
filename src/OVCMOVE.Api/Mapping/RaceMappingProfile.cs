using AutoMapper;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.UpdateRace;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Api.Mapping;

public class RaceMappingProfile : Profile
{
    public RaceMappingProfile()
    {
        CreateMap<CreateRaceCommand, Race>()
            .ForMember(dest => dest.RaceName, opt => opt.MapFrom(src => src.RaceName.Trim()))
            .ForMember(dest => dest.Place, opt => opt.MapFrom(src => src.Place.Trim()))
            .ForMember(dest => dest.CoverUrl, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.CoverUrl) ? null : src.CoverUrl.Trim()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => NormalizeRaceStatus(src.Status)))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateRaceCommand, Race>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.RaceId))
            .ForMember(dest => dest.RaceName, opt => opt.MapFrom(src => src.RaceName.Trim()))
            .ForMember(dest => dest.Place, opt => opt.MapFrom(src => src.Place.Trim()))
            .ForMember(dest => dest.CoverUrl, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.CoverUrl) ? null : src.CoverUrl.Trim()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => NormalizeRaceStatus(src.Status)))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        CreateMap<RaceDto.BoothInput, Booth>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Place, opt => opt.MapFrom(src => src.Place.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Trim()))
            .ForMember(dest => dest.BoothOrganizerID, opt => opt.MapFrom(src => src.OrganizerID.Trim()));

        CreateMap<RaceDto.RaceTeamInputDto, RaceTeam>()
            .ForMember(dest => dest.TeamID, opt => opt.MapFrom(src => src.TeamID));

        CreateMap<Guid, RaceOrganizer>()
            .ForMember(dest => dest.OrganizerID, opt => opt.MapFrom(src => src));

        CreateMap<Race, GetAllRacesResultModel>();
    }

    private static string NormalizeRaceStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return RaceConstants.RaceStatus.Draft;

        return status.Trim().ToLowerInvariant() switch
        {
            RaceConstants.RaceStatus.Ready => RaceConstants.RaceStatus.Ready,
            RaceConstants.RaceStatus.Ongoing => RaceConstants.RaceStatus.Ongoing,
            RaceConstants.RaceStatus.Paused => RaceConstants.RaceStatus.Paused,
            RaceConstants.RaceStatus.Completed => RaceConstants.RaceStatus.Completed,
            _ => RaceConstants.RaceStatus.Draft,
        };
    }
}
