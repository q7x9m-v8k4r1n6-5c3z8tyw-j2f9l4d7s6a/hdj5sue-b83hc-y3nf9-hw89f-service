using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Booths.Query.GetMyBooth;

public class GetMyBoothQueryHandler : IRequestHandler<GetMyBoothQuery, MyBoothResultModel?>
{
    private readonly IBoothOrganizerRepository _boothOrganizerRepository;
    private readonly IBoothRepository _boothRepository;

    public GetMyBoothQueryHandler(
        IBoothOrganizerRepository boothOrganizerRepository,
        IBoothRepository boothRepository)
    {
        _boothOrganizerRepository = boothOrganizerRepository;
        _boothRepository = boothRepository;
    }

    public async Task<MyBoothResultModel?> Handle(GetMyBoothQuery request, CancellationToken cancellationToken)
    {
        var assignment = await _boothOrganizerRepository.GetByOrganizerAndRaceAsync(
            request.OrganizerId, request.RaceId, cancellationToken);
        if (assignment is null) return null;

        var booth = await _boothRepository.GetByIdAsync(assignment.BoothId, cancellationToken);
        if (booth is null) return null;

        return new MyBoothResultModel
        {
            BoothId = booth.Id,
            Name = booth.Name,
            Place = booth.Place,
            Description = booth.Description
        };
    }
}