using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Teams.Query.GetMySession;

public sealed class GetMySessionQueryHandler(
    IBoothRepository boothRepository)
    : IRequestHandler<GetMySessionQuery, MySessionResultModel?>
{
    public async Task<MySessionResultModel?> Handle(
        GetMySessionQuery request,
        CancellationToken cancellationToken)
    {
        var booth = await boothRepository.GetActiveByTeamAndRaceAsync(
            request.TeamId,
            request.RaceId,
            cancellationToken);

        return booth is null
            ? null
            : new MySessionResultModel
            {
                RaceId = booth.RaceId,
                BoothId = booth.Id,
                BoothName = booth.Name,
                Place = booth.Place,
                Description = booth.Description,
                IsHidden = booth.IsHidden,
                Status = booth.Status
            };
    }
}
