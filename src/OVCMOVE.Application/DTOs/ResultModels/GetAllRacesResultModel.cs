using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Races.Query.GetAllRaces;

public class GetAllRacesResultModel
{
    public Guid Id { get; set; }
    public string RaceName { get; set; } = string.Empty;
    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Place { get; set; } = string.Empty;
    public string Status { get; set; } = RaceConstants.RaceStatus.Upcoming;
    public string? CoverUrl { get; set; }
}