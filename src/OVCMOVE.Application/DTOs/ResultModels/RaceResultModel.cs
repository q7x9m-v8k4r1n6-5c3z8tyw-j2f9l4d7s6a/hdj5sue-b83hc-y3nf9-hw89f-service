using OVCMOVE.Application.DTOs.Race;

namespace OVCMOVE.Application.DTOs.ResultModels;

public class RaceItemResultModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RaceName { get; init; } = string.Empty;
    public DateTime TimeStart { get; init; }
    public DateTime TimeEnd { get; init; }
    public string Place { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? CoverUrl { get; init; }
}

public class RaceDetailResultModel : RaceItemResultModel
{
    public bool IsToggledLeaderboard { get; init; }
    public bool IsHiddenPoint { get; init; }
    public IReadOnlyCollection<Guid> OrganizerId { get; init; } = Array.Empty<Guid>();
    public IReadOnlyCollection<RaceDto.RaceTeamInputDto> RaceTeam { get; init; } = Array.Empty<RaceDto.RaceTeamInputDto>();
    public IReadOnlyCollection<RaceDto.BoothInput> Booth { get; init; } = Array.Empty<RaceDto.BoothInput>();
}
