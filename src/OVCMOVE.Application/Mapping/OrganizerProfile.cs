using AutoMapper;
using OVCMOVE.Application.DTOs.Organizer;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Mapping;

public class OrganizerProfile : Profile
{
    public OrganizerProfile()
    {
        CreateMap<Organizer, OrganizerResponse>();
    }
}
