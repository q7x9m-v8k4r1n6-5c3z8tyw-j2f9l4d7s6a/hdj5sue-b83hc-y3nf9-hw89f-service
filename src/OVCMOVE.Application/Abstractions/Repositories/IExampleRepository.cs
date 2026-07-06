using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IExampleRepository
{
    Task<ExampleResultModel.GetAllExamplesByFilterResultModel> GetAllByFilterAsync(CancellationToken cancellationToken = default);

    Task<ExampleResultModel.CreateExampleResultModel> CreateAsync(CancellationToken cancellationToken = default);
}
