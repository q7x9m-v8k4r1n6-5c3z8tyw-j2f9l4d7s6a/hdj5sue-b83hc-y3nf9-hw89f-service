using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Booths.Query.GetMyBooth;

namespace OVCMOVE.Api.Mapping;

public static class BoothContractMapping
{
    public static BoothContract.MyBoothResponse ToResponse(
        this MyBoothResultModel result) =>
        new()
        {
            BoothId = result.BoothId,
            Name = result.Name,
            Place = result.Place,
            Description = result.Description,
            Type = result.Type,
            MaximumScore = result.MaximumScore,
            Status = result.Status,
            TeamId = result.TeamId,
            TeamName = result.TeamName
        };
}
