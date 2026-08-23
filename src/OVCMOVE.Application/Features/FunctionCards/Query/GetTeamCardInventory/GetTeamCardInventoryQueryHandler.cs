using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardInventory;

public sealed class GetTeamCardInventoryQueryHandler(IFunctionCardRepository repository)
    : IRequestHandler<GetTeamCardInventoryQuery, IReadOnlyCollection<TeamCardInventoryItemModel>>
{
    public async Task<IReadOnlyCollection<TeamCardInventoryItemModel>> Handle(
        GetTeamCardInventoryQuery request,
        CancellationToken cancellationToken)
    {
        return await repository.GetByTeamIdAsync(
            request.RaceId, 
            request.TeamId, 
            WorkflowConstants.WorkflowStatus.Active, 
            cancellationToken);
    }
}