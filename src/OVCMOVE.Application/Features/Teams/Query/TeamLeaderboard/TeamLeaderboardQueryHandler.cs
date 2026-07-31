using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

public sealed class TeamLeaderboardQueryHandler(
    IRaceRepository raceRepository,
    IScoringLogRepository scoringLogRepository)
    : IRequestHandler<TeamLeaderboardQuery, TeamLeaderboardResultModel>
{
    public async Task<TeamLeaderboardResultModel> Handle(
        TeamLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var race = await raceRepository.GetByIdAsync(
            request.RaceId,
            cancellationToken)
            ?? throw new ApplicationNotFoundException("Giải đua không tồn tại.");

        var leaderboard = await raceRepository.GetLeaderboardAsync(
            request.RaceId,
            cancellationToken);
        var currentTeam = leaderboard.FirstOrDefault(
            entry => entry.TeamId == request.TeamId)
            ?? throw new ApplicationNotFoundException(
                "Đội không tham gia giải đua này.");

        var boothStats = await scoringLogRepository.GetCompletedBoothStatsAsync(
            request.RaceId,
            request.TeamId,
            cancellationToken);

        return new TeamLeaderboardResultModel
        {
            CurrentTeam = new TeamScoreSummaryResultModel
            {
                TeamId = currentTeam.TeamId,
                DisplayName = currentTeam.DisplayName,
                Rank = currentTeam.Rank,
                TotalScore = currentTeam.TotalScore,
                CompletedRegularBooths = boothStats.CompletedRegularBooths,
                CompletedHiddenBooths = boothStats.CompletedHiddenBooths
            },
            IsLeaderboardVisible = race.IsToggledLeaderboard,
            AreOtherTeamPointsHidden = race.IsHiddenPoint,
            Teams = race.IsToggledLeaderboard
                ? leaderboard.Select(entry =>
                {
                    var isCurrentTeam = entry.TeamId == request.TeamId;
                    return new TeamLeaderboardEntryResultModel
                    {
                        TeamId = entry.TeamId,
                        DisplayName = entry.DisplayName,
                        Rank = entry.Rank,
                        TotalScore = isCurrentTeam || !race.IsHiddenPoint
                            ? entry.TotalScore
                            : null,
                        IsCurrentTeam = isCurrentTeam
                    };
                }).ToArray()
                : Array.Empty<TeamLeaderboardEntryResultModel>()
        };
    }
}
