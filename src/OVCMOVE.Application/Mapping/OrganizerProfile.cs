using AutoMapper;
using OVCMOVE.Application.DTOs.Organizer;
using OVCMOVE.Application.Organizers.Commands;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Mapping;

public class OrganizerProfile : Profile
{
    public OrganizerProfile()
    {
        CreateMap<CreateOrganizerRequest, CreateOrganizerCommand>();
        CreateMap<Organizer, OrganizerResponse>();
    }
}
