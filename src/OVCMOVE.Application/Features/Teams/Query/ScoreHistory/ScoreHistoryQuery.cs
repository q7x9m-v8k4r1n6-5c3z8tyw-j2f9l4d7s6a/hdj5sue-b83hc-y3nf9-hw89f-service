using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.ScoreHistory;

public sealed record ScoreHistoryQuery(
    Guid RaceId,
    Guid TeamId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ScoreHistoryItemResultModel>>;
