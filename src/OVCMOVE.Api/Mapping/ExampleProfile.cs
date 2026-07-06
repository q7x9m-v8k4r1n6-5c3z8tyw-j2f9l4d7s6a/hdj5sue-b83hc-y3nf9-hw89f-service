using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Example.Query.GetAllExampleByFilter;

namespace OVCMOVE.Api.Mapping
{
    public class ExampleProfile : Profile
    {
        public ExampleProfile()
        {
            CreateMap<ExampleContract.ExmampleRequestModel, GetAllExampleByFilterQuery>();
        }
    }
}
