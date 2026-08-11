using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
namespace OVCMOVE.Application.Features.Booths.Query.GetMyBooth;

public class GetMyBoothQueryHandler : IRequestHandler<GetMyBoothQuery, MyBoothResultModel?>
{
    private readonly IBoothOrganizerRepository _boothOrganizerRepository;
    private readonly IBoothRepository _boothRepository;
    private readonly IUserRepository _userRepository;

    public GetMyBoothQueryHandler(
        IBoothOrganizerRepository boothOrganizerRepository,
        IBoothRepository boothRepository,
        IUserRepository userRepository)
    {
        _boothOrganizerRepository = boothOrganizerRepository;
        _boothRepository = boothRepository;
        _userRepository = userRepository;
    }

    public async Task<MyBoothResultModel?> Handle(GetMyBoothQuery request, CancellationToken cancellationToken)
    {
        var assignment = await _boothOrganizerRepository.GetByOrganizerAndRaceAsync(
            request.OrganizerId, request.RaceId, cancellationToken);
        if (assignment is null) return null;

        var booth = await _boothRepository.GetByIdAsync(assignment.BoothId, cancellationToken);
        if (booth is null) return null;

        var team = booth.TeamId.HasValue
            ? await _userRepository.GetByIdAsync(
                booth.TeamId.Value,
                cancellationToken)
            : null;

        return new MyBoothResultModel
        {
            BoothId = booth.Id,
            Name = booth.Name,
            Place = booth.Place,
            Description = booth.Description,
            Status = booth.Status,
            TeamId = booth.TeamId,
            TeamName = team?.DisplayName
        };
    }
}
