using MediatR;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Behaviors;

public class AuditActorBehavior<TRequest, TResponse>(
    ICurrentActorProvider currentActorProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Attaches the authenticated actor to audited commands.</summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is AuditedRequest auditedRequest)
        {
            auditedRequest.Actor = currentActorProvider.GetCurrentActor();
        }

        return await next();
    }
}
