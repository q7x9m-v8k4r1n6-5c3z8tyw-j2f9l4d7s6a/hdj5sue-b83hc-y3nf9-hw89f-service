using static OVCMOVE.Api.Contracts.CommonContract;

namespace OVCMOVE.Api.Contracts;

public static class RaceContract
{
    /// <summary>
    /// Get all races request model
    /// </summary>
    public class GetAllRacesRequest : PagedRequest
    {
    }

    /// <summary>
    /// Create new race request model
    /// </summary>
    public class CreateNewRaceRequest
    {
        public BasicInfoModel BasicInfo { get; set; } 
        public List<Guid>? OrganizerId { get; set; }
        public List<Guid>? RaceTeam { get; set; }
        public List<BoothInfoModel>? Booths { get; set; }
        public RaceSettingsModel RaceSettings { get; set; }

        public class BasicInfoModel
        {
            public string RaceName { get; set; } 
            public DateTime TimeStart { get; set; }
            public DateTime TimeEnd { get; set; }
            public string Place { get; set; } 
            public string? CoverUrl { get; set; }
        }

        public class BoothInfoModel
        {
            public string Name { get; set; } 
            public string Place { get; set; }
            public string? Description { get; set; }
            public List<Guid> OrganizerIds { get; set; }
        }

        public class RaceSettingsModel
        {
            public bool IsToggledLeaderboard { get; set; }
            public bool IsHiddenPoint { get; set; }
        }
    }

    public class PatchRaceRequest
    {
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
            public string Name { get; set; }
            public string Place { get; set; }
            public string? Description { get; set; }
            public List<Guid> OrganizerIds { get; set; }
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
}
