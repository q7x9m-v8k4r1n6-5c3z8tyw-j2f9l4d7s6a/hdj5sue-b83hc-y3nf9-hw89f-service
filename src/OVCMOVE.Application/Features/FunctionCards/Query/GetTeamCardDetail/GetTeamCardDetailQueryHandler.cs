using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.FunctionCards.Query.GetTeamCardDetail;

public sealed class GetTeamCardDetailQueryHandler(IFunctionCardRepository repository)
    : IRequestHandler<GetTeamCardDetailQuery, TeamCardDetailModel>
{
    public async Task<TeamCardDetailModel> Handle(
        GetTeamCardDetailQuery request,
        CancellationToken cancellationToken) =>
        new TeamCardDetailModel
        {
            CardInfo = await repository.GetCardDescriptionByIdAsync(
                request.CardId, 
                request.TeamId, 
                WorkflowConstants.WorkflowStatus.Draft, 
                cancellationToken)
                    ?? throw new ApplicationNotFoundException("Không tìm thấy thẻ hoặc bạn không có quyền truy cập thẻ này.")   
        };

}