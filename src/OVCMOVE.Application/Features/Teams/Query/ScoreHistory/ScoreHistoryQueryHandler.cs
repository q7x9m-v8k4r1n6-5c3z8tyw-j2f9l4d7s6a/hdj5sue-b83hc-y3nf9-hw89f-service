using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Teams.Query.ScoreHistory;

public sealed class ScoreHistoryQueryHandler(
    IRaceRepository raceRepository)
    : IRequestHandler<ScoreHistoryQuery, PagedResult<ScoreHistoryItemResultModel>>
{
    public async Task<PagedResult<ScoreHistoryItemResultModel>> Handle(
        ScoreHistoryQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = await raceRepository.GetByIdAsync(request.RaceId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Giải đua không tồn tại.");
        var currentScore = await raceRepository.GetRaceTeamScoreAsync(
            request.RaceId,
            request.TeamId,
            cancellationToken);
        if (currentScore is null)
        {
            throw new ApplicationNotFoundException(
                "Đội không tham gia giải đua này.");
        }

        var (page, pageSize) = Pagination.Normalize(
            request.Page,
            request.PageSize);
        var (logs, totalItems) = await raceRepository.GetScoringLogPageByRaceIdAsync(
            request.RaceId,
            request.TeamId,
            page,
            pageSize,
            cancellationToken);

        var items = logs.Select(log => new ScoreHistoryItemResultModel
        {
            Id = log.LogId,
            BoothId = log.BoothId,
            OrganizerId = log.ActorId,
            ScoreGiven = log.ScoreDelta,
            ScoreAfterChange = log.ScoreAfter,
            Source = ToTeamHistorySource(log),
            Reason = log.Reason,
            CreatedAt = log.CreatedAt
        }).ToArray();

        return new PagedResult<ScoreHistoryItemResultModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    private static string ToTeamHistorySource(
        ScoringLogResultModel log) =>
        log.ReasonCode switch
        {
            ScoringLogConstants.ReasonCode.BoothCompleted =>
                ScoringLogConstants.Source.BoothCompleted,
            ScoringLogConstants.ReasonCode.Manual =>
                ScoringLogConstants.Source.AdminFix,
            _ => log.EventCode
        };
}
