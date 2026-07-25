using AutoMapper;
using OVCMOVE.Application.DTOs.Team;
using OVCMOVE.Application.Features.Teams.Command.CreateTeam;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;

namespace OVCMOVE.Application.Mapping;

public class TeamProfile : Profile
{
    public TeamProfile()
    {
        CreateMap<CreateTeamRequest, CreateTeamCommand>();
        CreateMap<TeamAccountDetails, GetAllTeamsResultModel>();
        CreateMap<TeamAccountDetails, SearchTeamResultModel>();
    }
}
