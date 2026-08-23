using MediatR;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetRaceCardsForAdmin;

public sealed record GetRaceCardsForAdminQuery(Guid RaceId)
    : IRequest<IReadOnlyCollection<FunctionCardResultModel>>;