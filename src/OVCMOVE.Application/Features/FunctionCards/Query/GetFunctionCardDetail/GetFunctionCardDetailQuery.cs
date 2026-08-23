using MediatR;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetFunctionCardDetail;

public sealed record GetFunctionCardDetailQuery(Guid CardId)
    : IRequest<FunctionCardResultModel>;