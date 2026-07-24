using MediatR;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Command.UpdateRace;

public class UpdateRaceCommand : IRequest<RaceDetailResultModel?>
{
    public Guid RaceId { get; set; }
    public string RaceName { get; set; } = string.Empty;
    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Place { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string? Status { get; set; }
    public bool IsToggledLeaderboard { get; set; }
    public bool IsHiddenPoint { get; set; }
    public List<Guid?> OrganizerId { get; set; } = new();
    public List<RaceDto.RaceTeamInputDto> RaceTeam { get; set; } = new();
    public List<RaceDto.BoothInput> Booth { get; set; } = new();
}
