using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.GetMySession;
using OVCMOVE.Application.Features.Teams.Query.ScoreHistory;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;
using OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

namespace OVCMOVE.Api.Mapping;

public static class TeamContractMapping
{
    public static TeamContract.MySessionResponse ToResponse(
        this MySessionResultModel result) => new()
        {
            RaceId = result.RaceId,
            BoothId = result.BoothId,
            BoothName = result.BoothName,
            Place = result.Place,
            Description = result.Description,
            IsHidden = result.IsHidden,
            Status = result.Status
        };

    public static TeamContract.LeaderboardResponse ToResponse(
        this TeamLeaderboardResultModel result) => new()
        {
            CurrentTeam = new TeamContract.TeamScoreSummaryResponse
            {
                TeamId = result.CurrentTeam.TeamId,
                DisplayName = result.CurrentTeam.DisplayName,
                Rank = result.CurrentTeam.Rank,
                TotalScore = result.CurrentTeam.TotalScore,
                CompletedRegularBooths =
                    result.CurrentTeam.CompletedRegularBooths,
                CompletedHiddenBooths =
                    result.CurrentTeam.CompletedHiddenBooths
            },
            IsLeaderboardVisible = result.IsLeaderboardVisible,
            AreOtherTeamPointsHidden = result.AreOtherTeamPointsHidden,
            Teams = result.Teams.Select(item =>
                new TeamContract.LeaderboardEntryResponse
                {
                    TeamId = item.TeamId,
                    DisplayName = item.DisplayName,
                    Rank = item.Rank,
                    TotalScore = item.TotalScore,
                    IsCurrentTeam = item.IsCurrentTeam
                }).ToArray()
        };

    public static TeamContract.ScoreHistoryItemResponse ToResponse(
        this ScoreHistoryItemResultModel result) => new()
        {
            Id = result.Id,
            BoothId = result.BoothId,
            OrganizerId = result.OrganizerId,
            ScoreGiven = result.ScoreGiven,
            ScoreAfterChange = result.ScoreAfterChange,
            Source = result.Source,
            Reason = result.Reason,
            CreatedAt = result.CreatedAt
        };

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
}
