using Microsoft.Extensions.Logging.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Features.FunctionCards.Common;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Workflows.Command;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class ExecuteWorkflowTransactionTests
{
    [Fact]
    public async Task Handle_retries_the_whole_transaction_and_commits_only_successful_attempt()
    {
        var workflowId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        var unitOfWork = new UnitOfWorkSpy();
        var repository = new WorkflowRepositoryDouble(
            CreateWorkflow(workflowId, cardId, raceId),
            unitOfWork,
            transientFailuresBeforeSuccess: 2);
        var cardRepository = new FunctionCardRepositoryDouble(
            new FunctionCard
            {
                Id = cardId,
                RaceId = raceId,
                CardKey = "shield",
                Name = "Shield",
                Category = "defense",
                InputsJson = "[]"
            });
        var transientDetector = new TransientErrorDetectorDouble();
        var realtimeBuffer = new WorkflowRealtimeBuffer();
        var runtime = new WorkflowRuntime(
            null!,
            repository,
            realtimeBuffer,
            transientDetector);
        var handler = new ExecuteWorkflowCommandHandler(
            repository,
            cardRepository,
            new WorkflowDefinitionValidator(),
            runtime,
            unitOfWork,
            new WorkflowRetryPolicy(transientDetector),
            realtimeBuffer,
            new WorkflowRealtimePublisher(
                new NotificationServiceDouble(),
                NullLogger<WorkflowRealtimePublisher>.Instance));

        var result = await handler.Handle(
            new ExecuteWorkflowCommand
            {
                WorkflowId = workflowId,
                Input = new WorkflowExecutionInputModel
                {
                    EventId = $"event:{Guid.NewGuid():N}"
                }
            },
            CancellationToken.None);

        Assert.Equal(WorkflowConstants.RunStatus.Succeeded, result.Status);
        Assert.Equal(WorkflowRetryPolicy.MaximumAttempts, repository.CompleteAttempts);
        Assert.Equal(WorkflowRetryPolicy.MaximumAttempts, unitOfWork.BeginCount);
        Assert.Equal(2, unitOfWork.RollbackCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.False(unitOfWork.HasActiveTransaction);
    }

    private static Workflow CreateWorkflow(Guid workflowId, Guid cardId, Guid raceId)
    {
        var definition = new WorkflowDefinitionModel
        {
            Nodes =
            [
                new WorkflowNodeModel
                {
                    Id = "trigger",
                    Type = WorkflowConstants.NodeType.TriggerActivated,
                    Config = WorkflowJson.ToElement(new { })
                },
                new WorkflowNodeModel
                {
                    Id = "stop",
                    Type = WorkflowConstants.NodeType.Stop,
                    Config = WorkflowJson.ToElement(new { })
                }
            ],
            Edges =
            [
                new WorkflowEdgeModel
                {
                    Id = "edge",
                    Source = "trigger",
                    Target = "stop"
                }
            ]
        };

        return new Workflow
        {
            Id = workflowId,
            CardId = cardId,
            RaceId = raceId,
            CardKey = "shield",
            CardName = "Shield",
            Name = "Shield workflow",
            TriggerType = WorkflowConstants.Trigger.Activated,
            Status = WorkflowConstants.Status.Published,
            Version = 1,
            DefinitionJson = System.Text.Json.JsonSerializer.Serialize(
                definition,
                WorkflowJson.Options)
        };
    }

    private sealed class WorkflowRepositoryDouble(
        Workflow workflow,
        UnitOfWorkSpy unitOfWork,
        int transientFailuresBeforeSuccess) : IWorkflowRepository
    {
        public int CompleteAttempts { get; private set; }

        public Task<Workflow?> GetByIdAsync(Guid workflowId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Workflow?>(workflow.Id == workflowId ? workflow : null);

        public Task CreateRunAsync(WorkflowRun run, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CompleteRunAsync(WorkflowRun run, CancellationToken cancellationToken = default)
        {
            Assert.True(unitOfWork.HasActiveTransaction);
            CompleteAttempts++;
            if (CompleteAttempts <= transientFailuresBeforeSuccess)
                throw new TransientTestException();

            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Workflow>> GetByRaceAsync(Guid raceId, string? cardKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Workflow>>([]);

        public Task CreateAsync(Workflow workflow, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(Workflow workflow, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SoftDeleteAsync(Guid workflowId, string actor, DateTime modifiedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<WorkflowRun>> GetRunsAsync(Guid workflowId, int limit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FunctionCardRepositoryDouble(FunctionCard card) :
        IFunctionCardRepository
    {
        public Task<FunctionCard?> GetByIdAsync(Guid cardId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FunctionCard?>(card.Id == cardId ? card : null);

        public Task<IReadOnlyCollection<FunctionCardReadRow>> GetByRaceAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FunctionCardReadRow?> GetDetailAsync(Guid cardId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<FunctionCard?> GetByKeyAsync(Guid raceId, string cardKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task CreateAsync(FunctionCard card, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(FunctionCard card, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> AssignTeamAsync(Guid cardId, Guid? teamId, string actor, DateTime expectedModifiedAt, DateTime modifiedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SoftDeleteAsync(Guid cardId, string actor, DateTime modifiedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TransientErrorDetectorDouble : ITransientErrorDetector
    {
        public bool IsTransient(Exception exception) =>
            exception is TransientTestException;
    }

    private sealed class TransientTestException : Exception;

    private sealed class NotificationServiceDouble : IBoothNotificationService
    {
        public Task NotifyBoothStatusChangedAsync(Guid raceId, Guid boothId, string status, Guid? teamId, string? teamName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceScoreChangedAsync(Guid raceId, Guid teamId, int delta, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyBoothEntryCancelledAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyBoothEntryRejectedAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceMessageAsync(Guid raceId, RaceMessageResultModel message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
