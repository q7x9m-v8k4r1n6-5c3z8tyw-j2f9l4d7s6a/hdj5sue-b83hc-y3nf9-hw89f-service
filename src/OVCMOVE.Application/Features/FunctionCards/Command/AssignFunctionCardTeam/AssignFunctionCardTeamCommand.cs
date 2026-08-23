using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.FunctionCards.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command.AssignFunctionCardTeam;

public sealed class AssignFunctionCardTeamCommand : AuditedRequest, IRequest<FunctionCardResultModel>
{
    public Guid CardId { get; init; }
    public Guid? TeamId { get; init; }
    public DateTime ExpectedModifiedAt { get; init; }
}

