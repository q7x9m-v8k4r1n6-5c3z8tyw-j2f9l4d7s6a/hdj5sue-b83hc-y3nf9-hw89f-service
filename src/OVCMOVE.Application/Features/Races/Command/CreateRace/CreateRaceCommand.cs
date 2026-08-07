using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races.Command.CreateRace;

public class CreateRaceCommand : AuditedRequest, IRequest<Guid>
{
    public string RaceName { get; set; } = string.Empty;
    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Place { get; set; } = string.Empty;
    public FileUploadModel? CoverImage { get; set; }
    public string Rules { get; set; } = string.Empty;
    public bool IsToggledLeaderboard { get; set; }
    public bool IsHiddenPoint { get; set; }
    public List<Guid> OrganizerIds { get; set; } = new();
    public List<Guid> TeamIds { get; set; } = new();
    public List<CreateBoothModel> Booths { get; set; } = new();

    public sealed class CreateBoothModel
    {
        public string Name { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<Guid> OrganizerIds { get; set; } = new();
    }
}

