using Dapper;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public sealed class WorkflowRepository(IDbExecutor db) : IWorkflowRepository
{
    public async Task<IReadOnlyCollection<Workflow>> GetByRaceAsync(
        Guid raceId,
        string? cardKey,
        CancellationToken cancellationToken = default) =>
        (await db.QueryAsync<Workflow>(
            WorkflowQueries.SelectByRace,
            new { RaceId = raceId, CardKey = cardKey },
            cancellationToken)).ToArray();

    public Task<Workflow?> GetByIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default) =>
        db.QueryFirstOrDefaultAsync<Workflow>(
            WorkflowQueries.SelectById,
            new { WorkflowId = workflowId },
            cancellationToken);

    public async Task CreateAsync(
        Workflow workflow,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await db.ExecuteAsync(
            WorkflowQueries.Insert,
            workflow,
            cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(Workflow));
    }

    public async Task<bool> UpdateAsync(
        Workflow workflow,
        DateTime expectedModifiedAt,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters(workflow);
        parameters.Add("ExpectedModifiedAt", expectedModifiedAt);
        return await db.ExecuteAsync(
            WorkflowQueries.Update,
            parameters,
            cancellationToken) == 1;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid workflowId,
        string actor,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default) =>
        await db.ExecuteAsync(
            WorkflowQueries.SoftDelete,
            new { WorkflowId = workflowId, Actor = actor, ModifiedAt = modifiedAt },
            cancellationToken) == 1;

    public async Task CreateRunAsync(
        WorkflowRun run,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await db.ExecuteAsync(
            WorkflowQueries.InsertRun,
            run,
            cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(WorkflowRun));
    }

    public async Task CompleteRunAsync(
        WorkflowRun run,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await db.ExecuteAsync(
            WorkflowQueries.CompleteRun,
            run,
            cancellationToken);
        if (affectedRows != 1)
            throw new InvalidOperationException("Workflow run could not be completed.");
    }

    public async Task<IReadOnlyCollection<WorkflowRun>> GetRunsAsync(
        Guid workflowId,
        int limit,
        CancellationToken cancellationToken = default) =>
        (await db.QueryAsync<WorkflowRun>(
            WorkflowQueries.SelectRuns,
            new { WorkflowId = workflowId, Limit = limit },
            cancellationToken)).ToArray();
}
