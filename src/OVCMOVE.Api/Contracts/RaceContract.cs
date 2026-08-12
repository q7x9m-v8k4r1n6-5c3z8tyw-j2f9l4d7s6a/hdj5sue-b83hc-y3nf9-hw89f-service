using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

using static OVCMOVE.Api.Contracts.CommonContract;

namespace OVCMOVE.Api.Contracts;

public static class RaceContract
{
    public sealed class RaceMutationFormRequest
    {
        public string Payload { get; set; } = string.Empty;
        public IFormFile? CoverImage { get; set; }
    }

    /// <summary>
    /// Get all races request model
    /// </summary>
    public class GetAllRacesRequest : PagedRequest
    {
        public Guid? TeamId { get; set; }
    }

    /// <summary>
    /// Create new race request model
    /// </summary>
    public class CreateNewRaceRequest
    {
        public BasicInfoModel BasicInfo { get; set; } = new();
        public List<Guid> OrganizerId { get; set; } = new();
        // A race may be created before any team is assigned.
        public List<Guid> RaceTeam { get; set; } = new();
        public List<BoothInfoModel>? Booths { get; set; }
        public RaceSettingsModel RaceSettings { get; set; } = new();

        public class BasicInfoModel
        {
            public string RaceName { get; set; } = string.Empty;
            public DateTime TimeStart { get; set; }
            public DateTime TimeEnd { get; set; }
            public string Place { get; set; } = string.Empty;
            public string? Rules {  get; set; } = string.Empty;
        }

        public class BoothInfoModel
        {
            public string Name { get; set; } = string.Empty;
            public string Place { get; set; } = string.Empty;
            public string? Description { get; set; }
            public List<Guid> OrganizerIds { get; set; } = new();
        }

        public class RaceSettingsModel
        {
            public bool IsToggledLeaderboard { get; set; }
            public bool IsHiddenPoint { get; set; }
        }
    }

    public class PatchRaceRequest
    {
        public DateTime ExpectedModifiedAt { get; set; }
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
            public string? Status { get; set; }
            public string? Rules { get; set; }
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

    public class RaceItemResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string RaceName { get; init; } = string.Empty;
        public DateTime TimeStart { get; init; }
        public DateTime TimeEnd { get; init; }
        public string Place { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? CoverUrl { get; init; }
        public DateTime ModifiedAt { get; init; }
    }

    public sealed class RaceDetailResponse : RaceItemResponse
    {
        public bool IsToggledLeaderboard { get; init; }
        public bool IsHiddenPoint { get; init; }
        public IReadOnlyCollection<Guid> OrganizerId { get; init; } = [];
        public IReadOnlyCollection<OrganizerResponse> Organizers { get; init; } = [];
        public IReadOnlyCollection<TeamResponse> RaceTeam { get; init; } = [];
        public IReadOnlyCollection<BoothResponse> Booth { get; init; } = [];
    }

    public sealed class OrganizerResponse
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
    }

    public sealed class TeamResponse
    {
        public Guid TeamID { get; init; }
        public string Name { get; init; } = string.Empty;
        public string LeaderEmail { get; init; } = string.Empty;
    }

    public sealed class BoothResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Place { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string OrganizerID { get; init; } = string.Empty;
    }

    public class TeamLeaderboardRequest
    {
        [Required(ErrorMessage = "Thiếu RaceId để lấy bảng xếp hạng.")]
        public Guid? RaceId { get; init; }
    }
    public class TeamLeaderboardResponse
    {
        public Guid TeamId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public int TotalScore { get; init; }
    }

    public class UpdateTeamScoreRequest
    {
        [Range(-10000, 10000, ErrorMessage = "Điểm điều chỉnh không hợp lệ.")]
        public int Delta { get; init; }

        [Required(ErrorMessage = "Thiếu lý do sửa điểm.")]
        public string Reason { get; init; } = string.Empty;
    }

    public class UpdateTeamScoreResponse
    {
        public Guid RaceId { get; init; }
        public Guid TeamId { get; init; }
        public int ScoreBefore { get; init; }
        public int ScoreAfter { get; init; }
        public int Delta { get; init; }
    }

    public class BoothListRequest
    {
        [Required(ErrorMessage = "thiếu RaceId để lấy danh sách các booth")]
        public Guid? RaceId { get; init; }
    }

    public class BoothListResponse
    {
        public Guid BoothId { get; init; }
        public string BoothName { get; init; } = string.Empty;
        public string BoothLocation {get; init; } = string.Empty;
        public string Description {get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public bool isHidden { get; init; } = false;
        public string? CurrentTeamName { get; init; }
        public string? CurrentOrganizerName { get; init; }
    }

    public class ScoringLogRequest : PagedRequest
    {
        [Required(ErrorMessage = "Thiếu RaceId để lấy log lịch sử điểm")]
        public Guid? RaceId { get; init; }
    }
    public class ScoringLogResponse
    {
        public Guid LogId { get; init;}
        public string? BoothName {get; init;} // null if actor != organizer
        public string EventName {get; set;} = string.Empty;
        public string TeamName {get; init;}= string.Empty;
        public string? ActorFullName {get; init;}
        public string? ActorShortName {get; init;}
        public int ScoreDelta {get; init;}
        public int ScoreBefore {get; init;}
        public int ScoreAfter {get; init;}
        public string Reason {get; set;} = string.Empty;
        public DateTime CreatedAt {get; init;}
        public string CreatedBy {get; init;} = string.Empty;
    }

    public sealed class RaceMessageRecipientRequest
    {
        [Required(ErrorMessage = "Thiếu mã người nhận.")]
        public string Key { get; init; } = string.Empty;

        [Required(ErrorMessage = "Thiếu tên người nhận.")]
        public string Label { get; init; } = string.Empty;

        [Required(ErrorMessage = "Thiếu loại người nhận.")]
        public string Type { get; init; } = string.Empty;
    }

    public sealed class SendRaceMessageRequest
    {
        [MinLength(1, ErrorMessage = "Cần chọn ít nhất một người nhận.")]
        public List<RaceMessageRecipientRequest> Recipients { get; init; } = [];

        [Required(ErrorMessage = "Nội dung tin nhắn không được để trống.")]
        public string Body { get; init; } = string.Empty;
    }

    public sealed class RaceMessageResponse
    {
        public Guid Id { get; init; }
        public Guid RaceId { get; init; }
        public Guid? SenderId { get; init; }
        public string SenderName { get; init; } = string.Empty;
        public IReadOnlyCollection<string> RecipientKeys { get; init; } = [];
        public IReadOnlyCollection<string> RecipientLabels { get; init; } = [];
        public string Body { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
