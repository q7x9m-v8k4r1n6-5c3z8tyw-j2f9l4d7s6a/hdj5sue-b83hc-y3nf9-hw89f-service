using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.ScoreHistory;

public sealed class ScoreHistoryQueryHandler(
    IRaceRepository raceRepository,
    IScoringLogRepository scoringLogRepository)
    : IRequestHandler<ScoreHistoryQuery, PagedResult<ScoreHistoryItemResultModel>>
{
    public async Task<PagedResult<ScoreHistoryItemResultModel>> Handle(
        ScoreHistoryQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = await raceRepository.GetByIdAsync(request.RaceId, cancellationToken)
            ?? throw new ApplicationNotFoundException("Giải đua không tồn tại.");
        var leaderboard = await raceRepository.GetLeaderboardAsync(
            request.RaceId,
            cancellationToken);
        if (!leaderboard.Any(entry => entry.TeamId == request.TeamId))
        {
            throw new ApplicationNotFoundException(
                "Đội không tham gia giải đua này.");
        }

        var (page, pageSize) = Pagination.Normalize(
            request.Page,
            request.PageSize);
        var (items, totalItems) = await scoringLogRepository.GetPageAsync(
            request.RaceId,
            request.TeamId,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<ScoreHistoryItemResultModel>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}
