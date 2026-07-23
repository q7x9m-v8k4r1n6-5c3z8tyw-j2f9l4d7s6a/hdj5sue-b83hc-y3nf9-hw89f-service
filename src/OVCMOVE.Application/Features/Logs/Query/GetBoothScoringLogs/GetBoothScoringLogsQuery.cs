using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Logs.Query.GetBoothScoringLogs;

public class GetBoothScoringLogsQuery : IRequest<List<GetBoothScoringLogsResultModel>>
{
    public int? Limit { get; set; }
}