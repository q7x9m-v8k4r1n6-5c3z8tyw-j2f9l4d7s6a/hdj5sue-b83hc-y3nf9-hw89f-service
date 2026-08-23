using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class DeleteWorkflowCommand : AuditedRequest, IRequest<bool>
{
    public Guid WorkflowId { get; init; }
}

public sealed class DeleteWorkflowCommandHandler(IWorkflowRepository repository)
    : IRequestHandler<DeleteWorkflowCommand, bool>
{
    public async Task<bool> Handle(
        DeleteWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        if (!await repository.SoftDeleteAsync(
            request.WorkflowId,
            request.GetActorOrSystem(),
            DateTime.UtcNow,
            cancellationToken))
            throw new ApplicationNotFoundException("Không tìm thấy workflow.");
        return true;
    }
}
