using AutoMapper;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;
using OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Api.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<Organizer, GetAllOrganizersResultModel>();
        CreateMap<Organizer, SearchOrganizerResultModel>();

        CreateMap<Team, GetAllTeamsResultModel>();
        CreateMap<Team, SearchTeamResultModel>();
    }
}