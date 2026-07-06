using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Example.Command.CreateNewExample
{
    public class CreateNewExampleCommandHandler :
        BaseCommandHandler<CreateNewExampleCommandHandler>,
        IRequestHandler<CreateNewExampleCommand, ExampleResultModel.CreateExampleResultModel>
    {
        private readonly IExampleRepository _exampleRepository;

        public CreateNewExampleCommandHandler(ILogger<CreateNewExampleCommandHandler> logger, IExampleRepository exampleRepository) : base(logger)
        {
            _exampleRepository = exampleRepository;
        }

        public async Task<ExampleResultModel.CreateExampleResultModel> Handle(CreateNewExampleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _exampleRepository.CreateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while handling CreateNewExampleCommand: {Message}", ex.Message);
                throw;
            }
        }
    }
}