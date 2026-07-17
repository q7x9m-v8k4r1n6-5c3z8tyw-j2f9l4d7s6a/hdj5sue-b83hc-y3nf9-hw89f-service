using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class ExampleRepository : BaseRepository<ExampleRepository>, IExampleRepository
{
    public ExampleRepository(ILogger<ExampleRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<ExampleResultModel.GetAllExamplesByFilterResultModel> GetAllByFilterAsync(CancellationToken cancellationToken = default)
    {
        var items = (await _dapperHelper.QueryAsync<ExampleResultModel.ExampleItemResultModel>(
            ExampleQueryHelper.GetAllExampleByFilterQuery()))
            .ToArray();

        return new ExampleResultModel.GetAllExamplesByFilterResultModel
        {
            TotalCount = items.Length,
            Items = items
        };
    }

    public Task<ExampleResultModel.CreateExampleResultModel> CreateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ExampleResultModel.CreateExampleResultModel
        {
            IsSuccess = true,
            Message = "Example command completed successfully."
        });
    }
}