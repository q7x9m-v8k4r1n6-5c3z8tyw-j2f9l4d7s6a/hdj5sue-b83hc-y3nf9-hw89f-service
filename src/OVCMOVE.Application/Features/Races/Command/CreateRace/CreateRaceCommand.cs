using MediatR;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Command.CreateRace;

public class CreateRaceCommand : IRequest<Guid?>
{
    /// <summary>
    /// Đại diện cho một request tạo mới 1 race
    /// </summary>
    public string RaceName { get; set; } = string.Empty;
    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Place { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string? Status { get; set; }

    public bool IsToggledLeaderboard { get; set; }
    public bool IsHiddenPoint { get; set; }
    /// <summary>
    /// Danh sách các thông tin cho các tab 2 3 4 khi tạo mới 1 race (Booth, BTC, Team)
    /// </summary>
    public List<Guid?> OrganizerId { get; set; } = new();
    public List<RaceDto.RaceTeamInputDto> RaceTeam { get; set; } = new();
    public List<RaceDto.BoothInput> Booth { get; set; } = new();
}
    
