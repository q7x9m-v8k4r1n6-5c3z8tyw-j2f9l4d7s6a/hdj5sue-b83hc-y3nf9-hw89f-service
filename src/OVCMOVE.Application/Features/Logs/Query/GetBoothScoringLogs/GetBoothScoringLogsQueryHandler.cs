using MediatR;
using Microsoft.Extensions.Logging;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Logs.Query.GetBoothScoringLogs;

public class GetBoothScoringLogsQueryHandler : 
    BaseQueryHandler<GetBoothScoringLogsQueryHandler>,
    IRequestHandler<GetBoothScoringLogsQuery, List<GetBoothScoringLogsResultModel>>
{
    private readonly IBoothScoringLogRepository _repository;

    public GetBoothScoringLogsQueryHandler(
        ILogger<GetBoothScoringLogsQueryHandler> logger,
        IBoothScoringLogRepository repository) : base(logger)
    {
        _repository = repository;
    }

    public async Task<List<GetBoothScoringLogsResultModel>> Handle(GetBoothScoringLogsQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _repository.GetScoringLogsAsync(request.Limit, cancellationToken);
    }
}