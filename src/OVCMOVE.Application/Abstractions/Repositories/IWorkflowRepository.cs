using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IWorkflowRepository
{
    Task<IReadOnlyCollection<Workflow>> GetByRaceAsync(
        Guid raceId,
        string? cardKey,
        CancellationToken cancellationToken = default);

    Task<Workflow?> GetByIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Workflow workflow,
        DateTime expectedModifiedAt,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        Guid workflowId,
        string actor,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default);

    Task CreateRunAsync(
        WorkflowRun run,
        CancellationToken cancellationToken = default);

    Task CompleteRunAsync(
        WorkflowRun run,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowRun>> GetRunsAsync(
        Guid workflowId,
        int limit,
        CancellationToken cancellationToken = default);
}
