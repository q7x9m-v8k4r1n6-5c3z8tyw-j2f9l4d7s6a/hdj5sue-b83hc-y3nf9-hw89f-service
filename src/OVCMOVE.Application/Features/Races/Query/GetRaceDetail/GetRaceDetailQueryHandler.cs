using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceDetail;

public class GetRaceDetailQueryHandler :
    IRequestHandler<GetRaceDetailQuery, RaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IRaceTeamRepository _raceTeamRepository;

    public GetRaceDetailQueryHandler(
        IRaceRepository raceRepository,
        IRaceTeamRepository raceTeamRepository)
    {
        _raceRepository = raceRepository;
        _raceTeamRepository = raceTeamRepository;
    }

    /// <summary>Returns the complete race view or null when the race is missing.</summary>
    public async Task<RaceDetailResultModel?> Handle(GetRaceDetailQuery request, CancellationToken cancellationToken)
    {
        if (request.TeamId.HasValue)
        {
            var assignedTeamIds = await _raceTeamRepository.GetTeamIdsByRaceIdAsync(
                request.RaceId,
                cancellationToken);
            if (!assignedTeamIds.Contains(request.TeamId.Value))
            {
                return null;
            }
        }

        var detail = await _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        if (!request.IsParticipantView)
        {
            return detail;
        }

        var booths = detail.Booth.AsEnumerable();
        if (!detail.IsShowHiddenBooths)
        {
            booths = booths.Where(b => !b.IsHidden);
        }

        var maskedBooths = booths.Select(b => new RaceBoothModel
        {
            Id = b.Id,
            Name = b.Name,
            Place = b.Place,
            Description = detail.IsHideBoothDescription ? string.Empty : b.Description,
            IsHidden = b.IsHidden,
            Status = detail.IsDisabledBoothStatus ? BoothConstants.BoothStatus.Occupied : b.Status,
            Type = b.Type,
            MaximumScore = b.MaximumScore,
            MapX = b.MapX,
            MapY = b.MapY,
            OrganizerIds = b.OrganizerIds
        }).ToArray();

        return new RaceDetailResultModel
        {
            Id = detail.Id,
            Name = detail.Name,
            RaceName = detail.RaceName,
            TimeStart = detail.TimeStart,
            TimeEnd = detail.TimeEnd,
            Place = detail.Place,
            Status = detail.Status,
            CoverUrl = detail.CoverUrl,
            MapImageUrl = detail.MapImageUrl,
            ModifiedAt = detail.ModifiedAt,
            IsToggledLeaderboard = detail.IsToggledLeaderboard,
            IsHiddenPoint = detail.IsHiddenPoint,
            IsShowHiddenBooths = detail.IsShowHiddenBooths,
            IsHideBoothDescription = detail.IsHideBoothDescription,
            IsDisabledBoothStatus = detail.IsDisabledBoothStatus,
            OrganizerId = detail.OrganizerId,
            Organizers = detail.Organizers,
            RaceTeam = detail.RaceTeam,
            Booth = maskedBooths
        };
    }
}
