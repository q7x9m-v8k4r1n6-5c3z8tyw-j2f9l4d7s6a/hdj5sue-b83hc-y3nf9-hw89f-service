using MediatR;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardDetail;

public sealed record GetTeamCardDetailQuery(Guid CardId, Guid TeamId) 
    : IRequest<TeamCardDetailModel>;