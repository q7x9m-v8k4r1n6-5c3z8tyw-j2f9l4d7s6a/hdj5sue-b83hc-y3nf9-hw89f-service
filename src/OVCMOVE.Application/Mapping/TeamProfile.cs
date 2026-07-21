using AutoMapper;
using OVCMOVE.Application.DTOs.Team;
using OVCMOVE.Application.Features.Teams.Command.CreateTeam;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Mapping;

public class TeamProfile : Profile
{
    public TeamProfile()
    {
        CreateMap<CreateTeamRequest, CreateTeamCommand>();
        CreateMap<Team, TeamResponse>();
    }
}
