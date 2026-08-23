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
        CancellationToken cancellationToken)
    {
        var description = await repository.GetCardDescriptionByIdAsync(
            request.CardId, 
            request.TeamId, 
            WorkflowConstants.WorkflowStatus.Active, 
            cancellationToken);

        if (description is null)
        {
            throw new ApplicationNotFoundException("Không tìm thấy thẻ hoặc bạn không có quyền truy cập thẻ này.");
        }

        return new TeamCardDetailModel { CardInfo = description };
    }
}