using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Options;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.SqlServer;
using OVCMOVE.Infrastructure.Repositories;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class CardEventReconciliationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Reconcile_CompletesClaimedMongoEffectWithoutReapplyingCommittedSqlScore()
    {
        var masterConnectionString = Environment.GetEnvironmentVariable(
            "OVCMOVE_TEST_SQLSERVER_MASTER");
        var mongoConnectionString = Environment.GetEnvironmentVariable(
            "OVCMOVE_TEST_MONGODB");
        if (string.IsNullOrWhiteSpace(masterConnectionString) ||
            string.IsNullOrWhiteSpace(mongoConnectionString))
            return;

        var sqlDatabaseName = $"ovcmove_test_{Guid.NewGuid():N}";
        var mongoDatabaseName = $"ovc_test_{Guid.NewGuid():N}"[..33];
        var masterBuilder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = "master"
        };
        var databaseBuilder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = sqlDatabaseName
        };
        var mongoClient = new MongoClient(mongoConnectionString);

        await WaitForSqlServerAsync(masterBuilder.ConnectionString);
        try
        {
            await ExecuteSqlAsync(
                masterBuilder.ConnectionString,
                $"CREATE DATABASE [{sqlDatabaseName}];");
            await ExecuteSqlAsync(databaseBuilder.ConnectionString, LegacyScoringLogSchema);
            var migration = await File.ReadAllTextAsync(Path.Combine(
                FindRepositoryRoot(),
                "sql",
                "008_CardEventReconciliation.sql"));
            await ExecuteSqlAsync(databaseBuilder.ConnectionString, migration);

            var sqlOptions = Options.Create(new DbConfigOptions
            {
                SqlServer = new DbConfigOptions.SqlServerOptions
                {
                    ConnectionString = databaseBuilder.ConnectionString
                }
            });
            var sqlFactory = new SqlServerFactory(sqlOptions);
            using var unitOfWork = new UnitOfWork(sqlFactory);
            var raceRepository = new RaceRepository(
                new DapperExecutor(sqlFactory, unitOfWork));

            var raceId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var boothId = Guid.NewGuid();
            var eventId = $"booth-result:{Guid.NewGuid():D}";
            var now = DateTime.UtcNow;
            await raceRepository.CreateScoringLogAsync(new ScoringLog
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                EventCode = ScoringLogConstants.EventCode.Booth,
                EventName = ScoringLogConstants.EventName.BoothScoring,
                RaceId = raceId,
                TeamId = teamId,
                BoothId = boothId,
                Delta = 20,
                ScoreBefore = 10,
                ScoreAfter = 30,
                ReasonCode = ScoringLogConstants.ReasonCode.BoothCompleted,
                Reason = ScoringLogConstants.Reason.BoothCompleted,
                CreatedAt = now,
                ModifiedAt = now,
                IsDeleted = false
            });

            var mongoDatabase = mongoClient.GetDatabase(mongoDatabaseName);
            var raceCollection = mongoDatabase.GetCollection<RaceCardDocument>("race_cards");
            var effectCollection = mongoDatabase.GetCollection<CardEffectDocument>("effect");
            var cardInstanceId = Guid.NewGuid().ToString();
            var cardUseId = Guid.NewGuid().ToString();
            var effect = new CardEffectDocument
            {
                RaceId = raceId.ToString(),
                CardId = CardIds.Cupid,
                CardInstanceId = cardInstanceId,
                CardUseId = cardUseId,
                OwnerTeamId = Guid.NewGuid().ToString(),
                TargetTeamId = teamId.ToString(),
                TriggerEventCode = CardEffectEventCodes.BoothResultFinalized,
                Status = CardEffectStatus.Active,
                ClaimedAt = now,
                ClaimedByEventId = eventId,
                StartAt = now,
                Data = new BsonDocument("timeBetweenUseMinutes", 15),
                CreatedAt = now,
                ModifiedAt = now
            };
            await raceCollection.InsertOneAsync(new RaceCardDocument
            {
                Id = raceId.ToString(),
                RaceId = raceId.ToString(),
                ModifiedAt = now,
                Teams =
                [
                    new RaceCardTeamState
                    {
                        TeamId = effect.OwnerTeamId,
                        TeamName = "Cupid owner",
                        Cards =
                        [
                            new TeamCardState
                            {
                                CardInfo = new TeamCardInfo
                                {
                                    CardInstanceId = cardInstanceId,
                                    CardId = CardIds.Cupid,
                                    CardUseCountRemain = 2
                                },
                                Status = CardStatus.Received,
                                ReceivedAt = now,
                                CardUses =
                                [
                                    new CardUseState
                                    {
                                        Id = cardUseId,
                                        EffectId = effect.Id,
                                        Status = CardUseStatus.Active,
                                        UseAt = now,
                                        CardUseCountBefore = 3,
                                        CardUseCountAfter = 2
                                    }
                                ]
                            }
                        ]
                    }
                ]
            });
            await effectCollection.InsertOneAsync(effect);

            var mongoRepository = new MongoRaceCardRepository(
                raceCollection,
                effectCollection);
            var service = new CardEventReconciliationService(
                mongoRepository,
                raceRepository,
                NullLogger<CardEventReconciliationService>.Instance);

            var response = await service.ReconcileAsync(
                raceId,
                eventId,
                Guid.NewGuid());

            Assert.Equal(1, response.ScoringLogCount);
            Assert.Equal(1, response.CompletedEffectCount);
            var sqlLogs = await raceRepository.GetScoringLogsByEventIdAsync(raceId, eventId);
            Assert.Single(sqlLogs);
            var storedEffect = await effectCollection.Find(item => item.Id == effect.Id).SingleAsync();
            var storedRace = await raceCollection.Find(item => item.Id == raceId.ToString()).SingleAsync();
            Assert.Equal(CardEffectStatus.Resolved, storedEffect.Status);
            Assert.Equal("reconciled_after_sql_commit", storedEffect.Resolution);
            Assert.Equal(
                CardUseStatus.Resolved,
                Assert.Single(Assert.Single(Assert.Single(storedRace.Teams).Cards).CardUses).Status);
        }
        finally
        {
            await mongoClient.DropDatabaseAsync(mongoDatabaseName);
            await ExecuteSqlAsync(
                masterBuilder.ConnectionString,
                $"IF DB_ID(N'{sqlDatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{sqlDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{sqlDatabaseName}]; END;");
        }
    }

    private static async Task WaitForSqlServerAsync(string connectionString)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return;
            }
            catch (Exception exception)
            {
                lastError = exception;
                if (attempt < 30)
                    await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new InvalidOperationException(
            "SQL Server did not become ready within 60 seconds.",
            lastError);
    }

    private static async Task ExecuteSqlAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "OVCMOVE.slnx")))
            directory = directory.Parent;
        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private const string LegacyScoringLogSchema = """
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
            [CreatedBy] NVARCHAR(100) NULL,
            [CreatedAt] DATETIME2(7) NOT NULL,
            [ModifiedBy] NVARCHAR(100) NULL,
            [ModifiedAt] DATETIME2(7) NOT NULL,
            [IsDeleted] BIT NOT NULL
        );
        """;
}
