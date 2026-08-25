using MediatR;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardInventory;

// Input mang đầy đủ ngữ cảnh: Race nào? Team nào?
public sealed record GetTeamCardInventoryQuery(Guid RaceId, Guid TeamId) 
    : IRequest<IReadOnlyCollection<TeamCardInventoryItemModel>>;