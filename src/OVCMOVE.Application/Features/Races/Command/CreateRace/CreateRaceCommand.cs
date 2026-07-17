using MediatR;
using OVCMOVE.Application.DTOs.Race;

namespace OVCMOVE.Application.Features.Races.Command.CreateRace;

public class CreateRaceCommand : IRequest<Guid?>
{
    public string RaceName { get; set; } = string.Empty;
    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Place { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public bool IsToggledLeaderboard { get; set; }
    public bool IsHiddenPoint { get; set; }
    public List<Guid?> OrganizerId { get; set; } = new();
    public List<RaceDto.RaceTeamInputDto> RaceTeam { get; set; } = new();
    public List<RaceDto.BoothInput> Booth { get; set; } = new();
}
