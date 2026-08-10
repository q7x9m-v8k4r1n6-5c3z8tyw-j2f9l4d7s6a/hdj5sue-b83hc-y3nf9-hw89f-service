using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;

namespace OVCMOVE.Api.Mapping;

public static class RaceContractMapping
{
    /// <summary>Maps race-list query parameters to the application query.</summary>
    public static GetAllRacesQuery ToQuery(
        this RaceContract.GetAllRacesRequest request) => new()
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TeamId = request.TeamId
        };

    /// <summary>Maps the create-race API contract to its application command.</summary>
    public static CreateRaceCommand ToCommand(
        this RaceContract.CreateNewRaceRequest request)
    {
        var basicInfo = request.BasicInfo
            ?? throw new ArgumentException(
                "BasicInfo không được để trống.");
        var settings = request.RaceSettings
            ?? throw new ArgumentException(
                "RaceSettings không được để trống.");

        return new CreateRaceCommand
        {
            RaceName = basicInfo.RaceName,
            TimeStart = basicInfo.TimeStart,
            TimeEnd = basicInfo.TimeEnd,
            Place = basicInfo.Place,
            Rules = basicInfo.Rules,
            IsToggledLeaderboard = settings.IsToggledLeaderboard,
            IsHiddenPoint = settings.IsHiddenPoint,
            OrganizerIds = request.OrganizerId ?? [],
            TeamIds = request.RaceTeam ?? [],
            Booths = (request.Booths ?? [])
                .Select(booth => new CreateRaceCommand.CreateBoothModel
                {
                    Name = booth.Name,
                    Place = booth.Place,
                    Description = booth.Description,
                    OrganizerIds = booth.OrganizerIds ?? []
                })
                .ToList()
        };
    }

    /// <summary>Maps the patch-race API contract to its application command.</summary>
    public static PatchRaceCommand ToCommand(
        this RaceContract.PatchRaceRequest request,
        Guid raceId) => new()
        {
            RaceId = raceId,
            ExpectedModifiedAt = request.ExpectedModifiedAt,
            BasicInfo = request.BasicInfo is null
            ? null
            : new PatchRaceCommand.BasicInfoPatchModel
            {
                RaceName = request.BasicInfo.RaceName,
                TimeStart = request.BasicInfo.TimeStart,
                TimeEnd = request.BasicInfo.TimeEnd,
                Place = request.BasicInfo.Place,
                Status = request.BasicInfo.Status,
                Rules = request.BasicInfo.Rules
            },
            RaceSettings = request.RaceSettings is null
            ? null
            : new PatchRaceCommand.RaceSettingsPatchModel
            {
                IsToggledLeaderboard =
                    request.RaceSettings.IsToggledLeaderboard,
                IsHiddenPoint = request.RaceSettings.IsHiddenPoint
            },
            Organizers = MapRelations(request.Organizers),
            RaceTeams = MapRelations(request.RaceTeams),
            Booths = MapBooths(request.Booths)
        };

    private static PatchRaceCommand.OrganizerPatchModel? MapRelations(
        RaceContract.PatchRaceRequest.OrganizerPatchModel? source) =>
        source is null
            ? null
            : new PatchRaceCommand.OrganizerPatchModel
            {
                Add = source.Add,
                Remove = source.Remove,
                Replace = MapReplacements(source.Replace)
            };

    private static PatchRaceCommand.RaceTeamPatchModel? MapRelations(
        RaceContract.PatchRaceRequest.RaceTeamPatchModel? source) =>
        source is null
            ? null
            : new PatchRaceCommand.RaceTeamPatchModel
            {
                Add = source.Add,
                Remove = source.Remove,
                Replace = MapReplacements(source.Replace)
            };

    private static List<PatchRaceCommand.ReplaceRelationItem>? MapReplacements(
        List<RaceContract.PatchRaceRequest.ReplaceRelationItem>? source) =>
        source?.Select(item => new PatchRaceCommand.ReplaceRelationItem
        {
            CurrentId = item.CurrentId,
            NewId = item.NewId
        }).ToList();

    private static PatchRaceCommand.BoothPatchModel? MapBooths(
        RaceContract.PatchRaceRequest.BoothPatchModel? source) =>
        source is null
            ? null
            : new PatchRaceCommand.BoothPatchModel
            {
                Add = source.Add?.Select(item =>
                    new PatchRaceCommand.CreateBoothPatchItem
                    {
                        Name = item.Name,
                        Place = item.Place,
                        Description = item.Description,
                        OrganizerIds = item.OrganizerIds ?? []
                    }).ToList(),
                Update = source.Update?.Select(item =>
                    new PatchRaceCommand.UpdateBoothPatchItem
                    {
                        BoothId = item.BoothId,
                        Name = item.Name,
                        Place = item.Place,
                        Description = item.Description,
                        OrganizerIds = item.OrganizerIds
                    }).ToList(),
                Remove = source.Remove
            };

    public static CommonContract.PagedResponse<
        RaceContract.RaceItemResponse> ToResponse(
        this PagedResult<RaceItemResultModel> result) =>
        result.ToResponse(MapRaceItem);

    public static RaceContract.RaceDetailResponse ToResponse(
        this RaceDetailResultModel result) => new()
        {
            Id = result.Id,
            Name = result.Name,
            RaceName = result.RaceName,
            TimeStart = result.TimeStart,
            TimeEnd = result.TimeEnd,
            Place = result.Place,
            Status = result.Status,
            CoverUrl = result.CoverUrl,
            ModifiedAt = result.ModifiedAt,
            IsToggledLeaderboard = result.IsToggledLeaderboard,
            IsHiddenPoint = result.IsHiddenPoint,
            OrganizerId = result.OrganizerId,
            Organizers = result.Organizers
            .Select(MapOrganizer)
            .ToArray(),
            RaceTeam = result.RaceTeam
            .Select(MapTeam)
            .ToArray(),
            Booth = result.Booth
            .Select(MapBooth)
            .ToArray()
        };

    private static RaceContract.RaceItemResponse MapRaceItem(
        RaceItemResultModel result) => new()
        {
            Id = result.Id,
            Name = result.Name,
            RaceName = result.RaceName,
            TimeStart = result.TimeStart,
            TimeEnd = result.TimeEnd,
            Place = result.Place,
            Status = result.Status,
            CoverUrl = result.CoverUrl,
            ModifiedAt = result.ModifiedAt
        };

    private static RaceContract.OrganizerResponse MapOrganizer(
        RaceOrganizerModel result) => new()
        {
            Id = result.Id,
            DisplayName = result.DisplayName,
            Email = result.Email,
            AvatarUrl = result.AvatarUrl
        };

    private static RaceContract.TeamResponse MapTeam(
        RaceTeamModel result) => new()
        {
            TeamID = result.TeamId,
            Name = result.Name,
            LeaderEmail = result.LeaderEmail
        };

    private static RaceContract.BoothResponse MapBooth(
        RaceBoothModel result) => new()
        {
            Id = result.Id,
            Name = result.Name,
            Place = result.Place,
            Description = result.Description,
            OrganizerID = string.Join(',', result.OrganizerIds)
        };

    public static TeamLeaderboardQuery ToQuery (
        this RaceContract.TeamLeaderboardRequest request) => new()
        {
            RaceId = request.RaceId
        };
    public static RaceContract.TeamLeaderboardResponse ToResponse(
        this TeamLeaderboardResultModel result) => new()
        {
            TeamId = result.TeamId,
            DisplayName = result.DisplayName,
            TotalScore = result.TotalScore
        };

    public static UpdateTeamScoreCommand ToCommand(
        this RaceContract.UpdateTeamScoreRequest request,
        Guid raceId,
        Guid teamId) => new()
        {
            RaceId = raceId,
            TeamId = teamId,
            Delta = request.Delta,
            Reason = request.Reason
        };

    public static RaceContract.UpdateTeamScoreResponse ToResponse(
        this UpdateTeamScoreResult result) => new()
        {
            RaceId = result.RaceId,
            TeamId = result.TeamId,
            ScoreBefore = result.ScoreBefore,
            ScoreAfter = result.ScoreAfter,
            Delta = result.Delta
        };

    public static SendRaceMessageCommand ToCommand(
        this RaceContract.SendRaceMessageRequest request,
        Guid raceId,
        Guid senderId,
        string senderName) => new()
        {
            RaceId = raceId,
            SenderId = senderId,
            SenderName = string.IsNullOrWhiteSpace(request.SenderName)
                ? senderName
                : request.SenderName.Trim(),
            Body = request.Body,
            Recipients = request.Recipients
                .Select(recipient => new RaceMessageRecipientModel
                {
                    Key = recipient.Key,
                    Label = recipient.Label,
                    Type = recipient.Type
                })
                .ToArray()
        };

    public static RaceContract.RaceMessageResponse ToResponse(
        this RaceMessageResultModel result) => new()
        {
            Id = result.Id,
            RaceId = result.RaceId,
            SenderId = result.SenderId,
            SenderName = result.SenderName,
            RecipientKeys = result.RecipientKeys,
            RecipientLabels = result.RecipientLabels,
            Body = result.Body,
            CreatedAt = result.CreatedAt
        };

    public static BoothListQuery ToQuery(
        this RaceContract.BoothListRequest request) => new()
        {
            RaceId = request.RaceId
        };

    public static RaceContract.BoothListResponse ToResponse(
        this BoothListResultModel result) => new()
        {
            BoothId = result.BoothId,
            BoothName = result.BoothName,
            BoothLocation = result.BoothLocation,
            Description = result.Description,
            Status = result.Status,
            isHidden = result.isHidden,
            CurrentTeamName = result.CurrentTeamName,
            CurrentOrganizerName = result.CurrentOrganizerName
        };

    public static ScoringLogQuery ToQuery(
        this RaceContract.ScoringLogRequest request) => new()
        {
            RaceId = request.RaceId.GetValueOrDefault(), 
            Page = request.Page,
            PageSize = request.PageSize
        };
    
    public static RaceContract.ScoringLogResponse ToResponse(
        this ScoringLogResultModel result) => new()
        {
            LogId = result.LogId,
            BoothName = result.BoothName,
            EventName = result.EventName,
            TeamName = result.TeamName,
            ActorFullName = result.ActorFullName,
            ActorShortName = result.ActorShortName,
            ScoreDelta = result.ScoreDelta,
            ScoreBefore = result.ScoreBefore,
            ScoreAfter = result.ScoreAfter,
            Reason = result.Reason,
            CreatedAt = result.CreatedAt,
            CreatedBy = result.CreatedBy
        };

    public static CommonContract.PagedResponse<RaceContract.ScoringLogResponse> ToResponse(
        this PagedResult<ScoringLogResultModel> result) =>
        result.ToResponse(item => item.ToResponse());
}
