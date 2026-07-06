using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Example.Query.GetAllExampleByFilter
{
    internal class GetAllExampleByFilterQueryHandler :
        BaseQueryHandler<GetAllExampleByFilterQueryHandler>,
        IRequestHandler<GetAllExampleByFilterQuery, DTOs.ResultModels.ExampleResultModel.GetAllExamplesByFilterResultModel>
    {
        private readonly IExampleRepository _exampleRepository;

        public GetAllExampleByFilterQueryHandler(
            ILogger<GetAllExampleByFilterQueryHandler> logger,
            IExampleRepository exampleRepository) : base(logger)
        {
            _exampleRepository = exampleRepository;
        }

        public Task<DTOs.ResultModels.ExampleResultModel.GetAllExamplesByFilterResultModel> Handle(GetAllExampleByFilterQuery request, CancellationToken cancellationToken)
        {
            return _exampleRepository.GetAllByFilterAsync(cancellationToken);
        }
    }
}
