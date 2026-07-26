using MediatR;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Behaviors;

public class AuditFieldsBehavior<TRequest, TResponse>(ICurrentActorProvider currentActorProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is BaseRequestModel auditRequest)
        {
            var actor = currentActorProvider.GetCurrentActor();
            var now = DateTime.UtcNow;

            auditRequest.CreatedBy ??= actor;
            auditRequest.CreatedAt ??= now;
            auditRequest.ModifiedBy = actor;
            auditRequest.ModifiedAt = now;
        }

        return await next();
    }
}
