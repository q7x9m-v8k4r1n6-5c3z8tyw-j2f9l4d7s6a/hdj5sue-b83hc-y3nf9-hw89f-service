using System.Diagnostics;
using System.Text.Json;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Workflows.Command;
using OVCMOVE.Application.Features.Workflows.Common;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.SqlServer;
using OVCMOVE.Infrastructure.Repositories;
using OVCMOVE.Infrastructure.Services;

namespace OVCMOVE.Test.Application;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class WorkflowSqlTransactionIntegrationTests : IAsyncLifetime
{
    private readonly string _databaseName = $"ovcmove_test_{Guid.NewGuid():N}";
    private string _masterConnectionString = string.Empty;
    private string _databaseConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        _masterConnectionString = GetMasterConnectionString();
        var builder = new SqlConnectionStringBuilder(_masterConnectionString)
        {
            InitialCatalog = "master",
            Encrypt = false,
            TrustServerCertificate = true,
            Pooling = false,
            ConnectTimeout = 30
        };
        _masterConnectionString = builder.ConnectionString;
        builder.InitialCatalog = _databaseName;
        _databaseConnectionString = builder.ConnectionString;

        await WaitUntilSqlServerIsReadyAsync(
            _masterConnectionString,
            TimeSpan.FromSeconds(60));

        try
        {
            await using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync($"CREATE DATABASE [{_databaseName}];");
            }

            await ExecuteDatabaseAsync(SchemaSql);
        }
        catch
        {
            await DropTemporaryDatabaseAsync();
            throw;
        }
    }

    public Task DisposeAsync() => DropTemporaryDatabaseAsync();

    [SqlServerIntegrationFact]
    [Trait("Category", "Integration")]
    public async Task Failed_second_score_action_rolls_back_first_score_and_scoring_log()
    {
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var missingTeamId = Guid.NewGuid();
        var workflow = CreateWorkflow(
            raceId,
            [
                ScoreAction("score-1", teamId, 20),
                ScoreAction("score-2", missingTeamId, 10)
            ]);
        await SeedAsync(workflow, teamId, initialScore: 100);

        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var notification = scope.ServiceProvider
            .GetRequiredService<RecordingNotificationService>();

        await Assert.ThrowsAsync<ApplicationNotFoundException>(
            () => sender.Send(new ExecuteWorkflowCommand
            {
                WorkflowId = workflow.Id,
                Input = new WorkflowExecutionInputModel
                {
                    EventId = $"rollback:{Guid.NewGuid():N}"
                }
            }));

        Assert.Equal(100, await GetScoreAsync(raceId, teamId));
        Assert.Equal(0, await CountScoringLogsAsync(raceId, teamId));
        Assert.Equal(
            WorkflowConstants.RunStatus.Failed,
            await GetLatestRunStatusAsync(workflow.Id));
        Assert.Empty(notification.ScoreEvents);
    }

    [SqlServerIntegrationFact]
    [Trait("Category", "Integration")]
    public async Task Successful_score_actions_commit_once_and_publish_realtime_after_commit()
    {
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var workflow = CreateWorkflow(
            raceId,
            [
                ScoreAction("score-1", teamId, 20),
                ScoreAction("score-2", teamId, 10)
            ]);
        await SeedAsync(workflow, teamId, initialScore: 100);

        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var notification = scope.ServiceProvider
            .GetRequiredService<RecordingNotificationService>();

        var result = await sender.Send(new ExecuteWorkflowCommand
        {
            WorkflowId = workflow.Id,
            Input = new WorkflowExecutionInputModel
            {
                EventId = $"success:{Guid.NewGuid():N}"
            }
        });

        Assert.Equal(WorkflowConstants.RunStatus.Succeeded, result.Status);
        Assert.Equal(130, await GetScoreAsync(raceId, teamId));
        Assert.Equal(2, await CountScoringLogsAsync(raceId, teamId));
        Assert.Equal(
            WorkflowConstants.RunStatus.Succeeded,
            await GetLatestRunStatusAsync(workflow.Id));
        Assert.Equal(2, notification.ScoreEvents.Count);
        Assert.All(notification.ScoreEvents, item =>
        {
            Assert.False(item.HadActiveTransaction);
            Assert.Equal(130, item.PersistedScore);
        });
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(
                typeof(ExecuteWorkflowCommand).Assembly));
        services.AddSingleton<ISqlServerFactory>(
            new TestSqlServerFactory(_databaseConnectionString));
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<UnitOfWork>());
        services.AddScoped<IDbExecutor, DapperExecutor>();
        services.AddScoped<IRaceRepository, RaceRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IFunctionCardRepository, FunctionCardRepository>();
        services.AddScoped<ITransientErrorDetector, SqlServerTransientErrorDetector>();
        services.AddScoped<WorkflowDefinitionValidator>();
        services.AddScoped<WorkflowRuntime>();
        services.AddScoped<WorkflowRetryPolicy>();
        services.AddScoped<WorkflowRealtimeBuffer>();
        services.AddScoped<WorkflowRealtimePublisher>();
        services.AddScoped<RecordingNotificationService>();
        services.AddScoped<IBoothNotificationService>(provider =>
            provider.GetRequiredService<RecordingNotificationService>());
        return services.BuildServiceProvider(validateScopes: true);
    }

    private async Task SeedAsync(Workflow workflow, Guid teamId, int initialScore)
    {
        var now = DateTime.UtcNow;
        var card = new FunctionCard
        {
            Id = workflow.CardId,
            RaceId = workflow.RaceId,
            CardKey = workflow.CardKey,
            Name = workflow.CardName,
            Description = string.Empty,
            Category = "effect",
            InputsJson = "[]",
            CreatedBy = "integration-test",
            CreatedAt = now,
            ModifiedBy = "integration-test",
            ModifiedAt = now
        };

        var factory = new TestSqlServerFactory(_databaseConnectionString);
        using var unitOfWork = new UnitOfWork(factory);
        var executor = new DapperExecutor(factory, unitOfWork);
        await new FunctionCardRepository(executor).CreateAsync(card);
        await new WorkflowRepository(executor).CreateAsync(workflow);
        await ExecuteDatabaseAsync(
            """
            INSERT INTO [dbo].[RaceTeam]
                ([Id], [RaceID], [TeamID], [TotalScore], [ModifiedBy], [ModifiedAt], [IsDeleted])
            VALUES
                (@Id, @RaceId, @TeamId, @TotalScore, N'integration-test', @ModifiedAt, 0);
            """,
            new
            {
                Id = Guid.NewGuid(),
                RaceId = workflow.RaceId,
                TeamId = teamId,
                TotalScore = initialScore,
                ModifiedAt = now
            });
    }

    private static Workflow CreateWorkflow(
        Guid raceId,
        IReadOnlyCollection<WorkflowNodeModel> actionNodes)
    {
        var cardId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var trigger = Node(
            "trigger",
            WorkflowConstants.NodeType.TriggerActivated,
            new { });
        var stop = Node("stop", WorkflowConstants.NodeType.Stop, new { });
        var nodes = new[] { trigger }.Concat(actionNodes).Append(stop).ToArray();
        var edges = nodes.Zip(nodes.Skip(1), (source, target) =>
            new WorkflowEdgeModel
            {
                Id = $"{source.Id}-{target.Id}",
                Source = source.Id,
                Target = target.Id
            }).ToArray();
        var definition = new WorkflowDefinitionModel
        {
            Nodes = nodes,
            Edges = edges
        };
        var now = DateTime.UtcNow;
        return new Workflow
        {
            Id = workflowId,
            CardId = cardId,
            RaceId = raceId,
            CardKey = $"score-{workflowId:N}",
            CardName = "Score workflow integration test",
            Name = "Score workflow integration test",
            Description = string.Empty,
            TriggerType = WorkflowConstants.Trigger.Activated,
            Status = WorkflowConstants.Status.Published,
            Version = 1,
            DefinitionJson = JsonSerializer.Serialize(definition, WorkflowJson.Options),
            CreatedBy = "integration-test",
            CreatedAt = now,
            ModifiedBy = "integration-test",
            ModifiedAt = now
        };
    }

    private static WorkflowNodeModel ScoreAction(
        string id,
        Guid teamId,
        int delta) => Node(
            id,
            WorkflowConstants.NodeType.AdjustScore,
            new
            {
                target = "custom",
                teamIds = new[] { teamId },
                delta,
                reason = "SQL transaction integration test"
            });

    private static WorkflowNodeModel Node(string id, string type, object config) =>
        new()
        {
            Id = id,
            Type = type,
            Config = WorkflowJson.ToElement(config)
        };

    private Task<int?> GetScoreAsync(Guid raceId, Guid teamId) =>
        QuerySingleAsync<int?>(
            """
            SELECT [TotalScore]
            FROM [dbo].[RaceTeam]
            WHERE [RaceID] = @RaceId AND [TeamID] = @TeamId AND [IsDeleted] = 0;
            """,
            new { RaceId = raceId, TeamId = teamId });

    private Task<int> CountScoringLogsAsync(Guid raceId, Guid teamId) =>
        QuerySingleAsync<int>(
            """
            SELECT COUNT(*)
            FROM [dbo].[ScoringLog]
            WHERE [RaceId] = @RaceId AND [TeamId] = @TeamId AND [IsDeleted] = 0;
            """,
            new { RaceId = raceId, TeamId = teamId });

    private Task<string> GetLatestRunStatusAsync(Guid workflowId) =>
        QuerySingleAsync<string>(
            """
            SELECT TOP (1) [Status]
            FROM [dbo].[WorkflowRuns]
            WHERE [WorkflowId] = @WorkflowId
            ORDER BY [StartedAt] DESC;
            """,
            new { WorkflowId = workflowId });

    private async Task<T> QuerySingleAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = new SqlConnection(_databaseConnectionString);
        return await connection.QuerySingleAsync<T>(sql, parameters);
    }

    private async Task ExecuteDatabaseAsync(string sql, object? parameters = null)
    {
        await using var connection = new SqlConnection(_databaseConnectionString);
        await connection.ExecuteAsync(sql, parameters);
    }

    private async Task DropTemporaryDatabaseAsync()
    {
        if (string.IsNullOrWhiteSpace(_masterConnectionString))
            return;

        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync($"""
            IF DB_ID(N'{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END;
            """);
    }

    private static async Task WaitUntilSqlServerIsReadyAsync(
        string connectionString,
        TimeSpan timeout)
    {
        var readinessConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 3,
            Pooling = false
        }.ConnectionString;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastException = null;

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                await using var connection = new SqlConnection(readinessConnectionString);
                await connection.OpenAsync();
                await connection.ExecuteScalarAsync<int>("SELECT 1;");
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastException = exception;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException(
            $"SQL Server was not ready after {timeout.TotalSeconds:0} seconds.",
            lastException);
    }

    private static string GetMasterConnectionString() =>
        Environment.GetEnvironmentVariable("OVCMOVE_TEST_SQLSERVER_MASTER")
        ?? throw new InvalidOperationException(
            "OVCMOVE_TEST_SQLSERVER_MASTER is required for SQL integration tests.");

    private sealed class TestSqlServerFactory(string connectionString) :
        ISqlServerFactory
    {
        public System.Data.IDbConnection CreateConnection() =>
            new SqlConnection(connectionString);
    }

    private sealed class RecordingNotificationService(
        IUnitOfWork unitOfWork,
        ISqlServerFactory connectionFactory) : IBoothNotificationService
    {
        public List<ScoreNotification> ScoreEvents { get; } = [];

        public async Task NotifyRaceScoreChangedAsync(
            Guid raceId,
            Guid teamId,
            int delta,
            CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            var score = await connection.QuerySingleAsync<int>(
                """
                SELECT [TotalScore]
                FROM [dbo].[RaceTeam]
                WHERE [RaceID] = @RaceId AND [TeamID] = @TeamId AND [IsDeleted] = 0;
                """,
                new { RaceId = raceId, TeamId = teamId });
            ScoreEvents.Add(new ScoreNotification(
                raceId,
                teamId,
                delta,
                unitOfWork.HasActiveTransaction,
                score));
        }

        public Task NotifyBoothStatusChangedAsync(Guid raceId, Guid boothId, string status, Guid? teamId, string? teamName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyBoothEntryCancelledAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyBoothEntryRejectedAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceMessageAsync(Guid raceId, RaceMessageResultModel message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed record ScoreNotification(
        Guid RaceId,
        Guid TeamId,
        int Delta,
        bool HadActiveTransaction,
        int PersistedScore);

    private const string SchemaSql = """
        CREATE TABLE [dbo].[FunctionCards]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [TeamId] UNIQUEIDENTIFIER NULL,
            [CardKey] NVARCHAR(100) NOT NULL,
            [Name] NVARCHAR(255) NOT NULL,
            [Description] NVARCHAR(1000) NOT NULL,
            [Category] NVARCHAR(30) NOT NULL,
            [BackgroundUrl] NVARCHAR(2048) NULL,
            [InputsJson] NVARCHAR(MAX) NOT NULL,
            [CreatedBy] NVARCHAR(255) NULL,
            [CreatedAt] DATETIME2 NOT NULL,
            [ModifiedBy] NVARCHAR(255) NULL,
            [ModifiedAt] DATETIME2 NOT NULL,
            [IsDeleted] BIT NOT NULL
        );

        CREATE TABLE [dbo].[Workflows]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            [CardId] UNIQUEIDENTIFIER NOT NULL,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [CardKey] NVARCHAR(100) NOT NULL,
            [CardName] NVARCHAR(255) NOT NULL,
            [Name] NVARCHAR(255) NOT NULL,
            [Description] NVARCHAR(1000) NOT NULL,
            [TriggerType] NVARCHAR(100) NOT NULL,
            [Status] NVARCHAR(30) NOT NULL,
            [Version] INT NOT NULL,
            [DefinitionJson] NVARCHAR(MAX) NOT NULL,
            [CreatedBy] NVARCHAR(255) NULL,
            [CreatedAt] DATETIME2 NOT NULL,
            [ModifiedBy] NVARCHAR(255) NULL,
            [ModifiedAt] DATETIME2 NOT NULL,
            [IsDeleted] BIT NOT NULL
        );

        CREATE TABLE [dbo].[WorkflowRuns]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            [WorkflowId] UNIQUEIDENTIFIER NOT NULL,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [CardKey] NVARCHAR(100) NOT NULL,
            [TriggerType] NVARCHAR(100) NOT NULL,
            [EventId] NVARCHAR(100) NULL,
            [Status] NVARCHAR(30) NOT NULL,
            [IsSimulation] BIT NOT NULL,
            [InputJson] NVARCHAR(MAX) NOT NULL,
            [OutputJson] NVARCHAR(MAX) NOT NULL,
            [Error] NVARCHAR(2000) NULL,
            [StartedAt] DATETIME2 NOT NULL,
            [CompletedAt] DATETIME2 NULL,
            [CreatedBy] NVARCHAR(255) NULL,
            [CreatedAt] DATETIME2 NOT NULL,
            [ModifiedBy] NVARCHAR(255) NULL,
            [ModifiedAt] DATETIME2 NOT NULL,
            [IsDeleted] BIT NOT NULL
        );
        CREATE UNIQUE INDEX [UX_WorkflowRuns_Workflow_Event]
            ON [dbo].[WorkflowRuns] ([WorkflowId], [EventId])
            WHERE [EventId] IS NOT NULL AND [IsDeleted] = 0;

        CREATE TABLE [dbo].[RaceTeam]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            [RaceID] UNIQUEIDENTIFIER NOT NULL,
            [TeamID] UNIQUEIDENTIFIER NOT NULL,
            [TotalScore] INT NOT NULL,
            [ModifiedBy] NVARCHAR(255) NULL,
            [ModifiedAt] DATETIME2 NOT NULL,
            [IsDeleted] BIT NOT NULL
        );

        CREATE TABLE [dbo].[ScoringLog]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            [EventCode] NVARCHAR(100) NOT NULL,
            [EventName] NVARCHAR(255) NOT NULL,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [TeamId] UNIQUEIDENTIFIER NOT NULL,
            [ActorId] UNIQUEIDENTIFIER NULL,
            [BoothId] UNIQUEIDENTIFIER NULL,
            [Delta] INT NOT NULL,
            [ScoreBefore] INT NOT NULL,
            [ScoreAfter] INT NOT NULL,
            [ReasonCode] NVARCHAR(100) NOT NULL,
            [Reason] NVARCHAR(500) NOT NULL,
            [CreatedBy] NVARCHAR(255) NULL,
            [CreatedAt] DATETIME2 NOT NULL,
            [ModifiedBy] NVARCHAR(255) NULL,
            [ModifiedAt] DATETIME2 NOT NULL,
            [IsDeleted] BIT NOT NULL
        );
        """;
}

public sealed class SqlServerIntegrationFactAttribute : FactAttribute
{
    public SqlServerIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("OVCMOVE_TEST_SQLSERVER_MASTER")))
        {
            Skip = "Set OVCMOVE_TEST_SQLSERVER_MASTER to run SQL Server integration tests.";
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerIntegrationCollection
{
    public const string Name = "SQL Server integration";
}
