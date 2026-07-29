using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;
using OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

namespace OVCMOVE.Api.Mapping;

public static class TeamContractMapping
{
    public static TeamContract.TeamListItemResponse ToResponse(
        this GetAllTeamsResultModel result) => new()
        {
            Id = result.Id,
            Name = result.Name,
            LeaderEmail = result.LeaderEmail,
            Username = result.Username,
            Status = result.Status
        };

    public static TeamContract.TeamSearchItemResponse ToResponse(
        this SearchTeamResultModel result) => new()
        {
            Id = result.Id,
            Name = result.Name,
            LeaderEmail = result.LeaderEmail
        };
    public static TeamLeaderboardQuery ToQuery (
        this TeamContract.TeamLeaderboardRequest request) => new()
        {
            RaceId = request.RaceId
        };
    public static TeamContract.TeamLeaderboardResponse ToResponse(
        this TeamLeaderboardResultModel result) => new()
        {
            DisplayName = result.DisplayName,
            TotalScore = result.TotalScore
        };
}
