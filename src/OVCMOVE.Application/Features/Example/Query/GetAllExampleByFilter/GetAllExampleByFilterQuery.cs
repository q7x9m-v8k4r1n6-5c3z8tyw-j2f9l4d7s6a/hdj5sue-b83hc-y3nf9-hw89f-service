using MediatR;

namespace OVCMOVE.Application.Features.Example.Query.GetAllExampleByFilter
{
    public class GetAllExampleByFilterQuery : IRequest<DTOs.ResultModels.ExampleResultModel.GetAllExamplesByFilterResultModel>
    {
    }
}
