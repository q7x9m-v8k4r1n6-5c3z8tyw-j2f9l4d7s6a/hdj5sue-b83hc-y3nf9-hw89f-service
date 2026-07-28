using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Api.Mapping;

public static class RaceContractMapping
{
    /// <summary>Maps race-list query parameters to the application query.</summary>
    public static GetAllRacesQuery ToQuery(
        this RaceContract.GetAllRacesRequest request) => new()
        {
            Page = request.Page,
            PageSize = request.PageSize
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
                Status = request.BasicInfo.Status
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
}
