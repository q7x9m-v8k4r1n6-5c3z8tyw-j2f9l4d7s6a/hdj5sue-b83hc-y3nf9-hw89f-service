using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Logs.Query.GetBoothScoringLogs;

namespace OVCMOVE.Api.Mapping;

public class LogsProfile : Profile
{
    public LogsProfile()
    {   
        // --- REQUEST ---
        CreateMap<LogsContract.BoothScoringLogsRequest, GetBoothScoringLogsQuery>();

        // --- RESPONSE ---
        CreateMap<GetBoothScoringLogsResultModel, LogsContract.BoothScoringLogsResponse>();
    }
}