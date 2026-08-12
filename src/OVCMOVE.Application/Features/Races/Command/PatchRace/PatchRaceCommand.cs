using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Command.PatchRace;

public class PatchRaceCommand : AuditedRequest, IRequest<RaceDetailResultModel?>
{
    public Guid RaceId { get; set; }
    public DateTime ExpectedModifiedAt { get; set; }
    public FileUploadModel? CoverImage { get; set; }
    public BasicInfoPatchModel? BasicInfo { get; set; }
    public RaceSettingsPatchModel? RaceSettings { get; set; }
    public OrganizerPatchModel? Organizers { get; set; }
    public RaceTeamPatchModel? RaceTeams { get; set; }
    public BoothPatchModel? Booths { get; set; }

    public class BasicInfoPatchModel
    {
        public string? RaceName { get; set; }
        public DateTime? TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string? Place { get; set; }
        public string? CoverUrl { get; set; }
        public string? Rules { get; set; }
        public string? Status { get; set; }
    }

    public class RaceSettingsPatchModel
    {
        public bool? IsToggledLeaderboard { get; set; }
        public bool? IsHiddenPoint { get; set; }
    }

    public class OrganizerPatchModel
    {
        public List<Guid>? Add { get; set; }
        public List<Guid>? Remove { get; set; }
        public List<ReplaceRelationItem>? Replace { get; set; }
    }

    public class RaceTeamPatchModel
    {
        public List<Guid>? Add { get; set; }
        public List<Guid>? Remove { get; set; }
        public List<ReplaceRelationItem>? Replace { get; set; }
    }

    public class ReplaceRelationItem
    {
        public Guid CurrentId { get; set; }
        public Guid NewId { get; set; }
    }

    public class BoothPatchModel
    {
        public List<CreateBoothPatchItem>? Add { get; set; }
        public List<UpdateBoothPatchItem>? Update { get; set; }
        public List<Guid>? Remove { get; set; }
    }

    public class CreateBoothPatchItem
    {
        public string Name { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsHidden { get; set; }
        public List<Guid> OrganizerIds { get; set; } = new();
    }

    public class UpdateBoothPatchItem
    {
        public Guid BoothId { get; set; }
        public string? Name { get; set; }
        public string? Place { get; set; }
        public string? Description { get; set; }
        public List<Guid>? OrganizerIds { get; set; }
    }
}
