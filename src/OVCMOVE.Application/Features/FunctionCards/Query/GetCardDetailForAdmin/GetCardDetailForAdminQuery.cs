using MediatR;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetCardDetailForAdmin;

public sealed record GetCardDetailForAdminQuery(Guid CardId)
    : IRequest<FunctionCardResultModel>;