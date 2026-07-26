using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;

namespace OVCMOVE.Api.Mapping;

public class RaceProfile : Profile
{
    public RaceProfile()
    {
        CreateMap<RaceContract.CreateNewRaceRequest, CreateRaceCommand>()
            .ForMember(dest => dest.RaceName, opt => opt.MapFrom(src => src.BasicInfo.RaceName))
            .ForMember(dest => dest.TimeStart, opt => opt.MapFrom(src => src.BasicInfo.TimeStart))
            .ForMember(dest => dest.TimeEnd, opt => opt.MapFrom(src => src.BasicInfo.TimeEnd))
            .ForMember(dest => dest.Place, opt => opt.MapFrom(src => src.BasicInfo.Place))
            .ForMember(dest => dest.CoverUrl, opt => opt.MapFrom(src => src.BasicInfo.CoverUrl))
            .ForMember(dest => dest.IsToggledLeaderboard, opt => opt.MapFrom(src => src.RaceSettings.IsToggledLeaderboard))
            .ForMember(dest => dest.IsHiddenPoint, opt => opt.MapFrom(src => src.RaceSettings.IsHiddenPoint))
            .ForMember(dest => dest.OrganizerId, opt => opt.MapFrom(src => src.OrganizerId == null
                ? new List<Guid?>()
                : src.OrganizerId.Select(id => (Guid?)id).ToList()))
            .ForMember(dest => dest.RaceTeam, opt => opt.MapFrom(src => src.RaceTeam == null
                ? new List<RaceDto.RaceTeamInputDto>()
                : src.RaceTeam.Select(id => new RaceDto.RaceTeamInputDto { TeamID = id }).ToList()))
            .ForMember(dest => dest.Booth, opt => opt.MapFrom(src => src.Booths ?? new List<RaceContract.CreateNewRaceRequest.BoothInfoModel>()));

        CreateMap<RaceContract.CreateNewRaceRequest.BoothInfoModel, RaceDto.BoothInput>()
            .ForMember(dest => dest.OrganizerID, opt => opt.MapFrom(src => string.Join(',', src.OrganizerIds)));

        CreateMap<RaceContract.PatchRaceRequest, PatchRaceCommand>();
        CreateMap<RaceContract.PatchRaceRequest.BasicInfoPatchModel, PatchRaceCommand.BasicInfoPatchModel>();
        CreateMap<RaceContract.PatchRaceRequest.RaceSettingsPatchModel, PatchRaceCommand.RaceSettingsPatchModel>();
        CreateMap<RaceContract.PatchRaceRequest.OrganizerPatchModel, PatchRaceCommand.OrganizerPatchModel>();
        CreateMap<RaceContract.PatchRaceRequest.RaceTeamPatchModel, PatchRaceCommand.RaceTeamPatchModel>();
        CreateMap<RaceContract.PatchRaceRequest.ReplaceRelationItem, PatchRaceCommand.ReplaceRelationItem>();
        CreateMap<RaceContract.PatchRaceRequest.BoothPatchModel, PatchRaceCommand.BoothPatchModel>();
        CreateMap<RaceContract.PatchRaceRequest.CreateBoothPatchItem, PatchRaceCommand.CreateBoothPatchItem>();
        CreateMap<RaceContract.PatchRaceRequest.UpdateBoothPatchItem, PatchRaceCommand.UpdateBoothPatchItem>();
    }
}
