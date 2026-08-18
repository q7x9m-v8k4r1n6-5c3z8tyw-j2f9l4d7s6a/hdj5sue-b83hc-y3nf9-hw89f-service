using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Workflows.Common;

namespace OVCMOVE.Application.Features.Workflows.Query;

public sealed record GetWorkflowsQuery(Guid RaceId, string? CardKey)
    : IRequest<IReadOnlyCollection<WorkflowResultModel>>;

public sealed record GetWorkflowDetailQuery(Guid WorkflowId)
    : IRequest<WorkflowResultModel>;

public sealed record GetWorkflowRunsQuery(Guid WorkflowId, int Limit)
    : IRequest<IReadOnlyCollection<WorkflowRunResultModel>>;

public sealed record GetWorkflowCatalogQuery()
    : IRequest<IReadOnlyCollection<WorkflowCatalogItemModel>>;

public sealed class GetWorkflowsQueryHandler(IWorkflowRepository repository)
    : IRequestHandler<GetWorkflowsQuery, IReadOnlyCollection<WorkflowResultModel>>
{
    public async Task<IReadOnlyCollection<WorkflowResultModel>> Handle(
        GetWorkflowsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId == Guid.Empty)
            throw new ApplicationValidationException("RaceId là bắt buộc.");
        return (await repository.GetByRaceAsync(
            request.RaceId,
            string.IsNullOrWhiteSpace(request.CardKey) ? null : request.CardKey.Trim(),
            cancellationToken)).Select(item => item.ToResult()).ToArray();
    }
}

public sealed class GetWorkflowDetailQueryHandler(IWorkflowRepository repository)
    : IRequestHandler<GetWorkflowDetailQuery, WorkflowResultModel>
{
    public async Task<WorkflowResultModel> Handle(
        GetWorkflowDetailQuery request,
        CancellationToken cancellationToken) =>
        (await repository.GetByIdAsync(request.WorkflowId, cancellationToken))?.ToResult()
        ?? throw new ApplicationNotFoundException("Không tìm thấy workflow.");
}

public sealed class GetWorkflowRunsQueryHandler(IWorkflowRepository repository)
    : IRequestHandler<GetWorkflowRunsQuery, IReadOnlyCollection<WorkflowRunResultModel>>
{
    public async Task<IReadOnlyCollection<WorkflowRunResultModel>> Handle(
        GetWorkflowRunsQuery request,
        CancellationToken cancellationToken) =>
        (await repository.GetRunsAsync(
            request.WorkflowId,
            Math.Clamp(request.Limit, 1, 100),
            cancellationToken)).Select(item => item.ToResult()).ToArray();
}

public sealed class GetWorkflowCatalogQueryHandler
    : IRequestHandler<GetWorkflowCatalogQuery, IReadOnlyCollection<WorkflowCatalogItemModel>>
{
    public Task<IReadOnlyCollection<WorkflowCatalogItemModel>> Handle(
        GetWorkflowCatalogQuery request,
        CancellationToken cancellationToken) => Task.FromResult(WorkflowCatalog.Items);
}
