using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;

namespace OVCMOVE.Api.Mapping;

public class TeamProfile : Profile
{
    public TeamProfile()
    { 
        CreateMap<TeamContract.GetTeamsRequest, GetAllTeamsQuery>();

        CreateMap<GetAllTeamsResultModel, TeamContract.GetTeamsResponse>();
        CreateMap<SearchTeamResultModel, TeamContract.SearchTeamResponse>();

        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
    }
}