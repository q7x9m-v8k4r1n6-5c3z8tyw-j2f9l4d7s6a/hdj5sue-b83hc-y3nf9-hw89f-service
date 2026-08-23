using MediatR;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetFunctionCards;

public sealed record GetFunctionCardsQuery(Guid RaceId)
    : IRequest<IReadOnlyCollection<FunctionCardResultModel>>;