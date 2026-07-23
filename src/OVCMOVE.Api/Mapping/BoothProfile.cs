using AutoMapper;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Query.GetBoothSummary;

namespace OVCMOVE.Api.Mapping;

public class BoothProfile : Profile
{
    public BoothProfile()
    {
        CreateMap<BoothContract.GetBoothSummaryRequest, GetBoothSummaryQuery>();
        CreateMap<GetBoothSummaryResultModel, BoothContract.GetBoothSummaryResponse>();

        CreateMap<GetBoothDetailResultModel, BoothContract.GetBoothDetailResponse>();
    }
}