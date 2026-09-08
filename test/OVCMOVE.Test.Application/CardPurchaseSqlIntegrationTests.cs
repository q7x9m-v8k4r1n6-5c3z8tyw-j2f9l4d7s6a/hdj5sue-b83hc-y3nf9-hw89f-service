using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Options;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.SqlServer;
using OVCMOVE.Infrastructure.Repositories;

namespace OVCMOVE.Test.Application;

public sealed class CardPurchaseSqlIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConcurrentDebits_CannotSpendTheSameScoreTwice()
    {
        var masterConnectionString = Environment.GetEnvironmentVariable("OVCMOVE_TEST_SQLSERVER_MASTER");
        if (string.IsNullOrWhiteSpace(masterConnectionString)) return;

        var databaseName = $"ovcmove_test_{Guid.NewGuid():N}";
        var master = new SqlConnectionStringBuilder(masterConnectionString) { InitialCatalog = "master" };
        var database = new SqlConnectionStringBuilder(masterConnectionString) { InitialCatalog = databaseName };
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        await WaitForSqlServerAsync(master.ConnectionString);
        try
        {
            await ExecuteSqlAsync(master.ConnectionString, $"CREATE DATABASE [{databaseName}];");
            await ExecuteSqlAsync(database.ConnectionString, Schema);
            await ExecuteSqlAsync(
                database.ConnectionString,
                $"INSERT INTO dbo.RaceTeam (RaceID, TeamID, TotalScore, ModifiedAt, IsDeleted) VALUES ('{raceId}', '{teamId}', 20, SYSUTCDATETIME(), 0);");

            var first = DebitAsync(database.ConnectionString, raceId, teamId, Guid.NewGuid());
            var second = DebitAsync(database.ConnectionString, raceId, teamId, Guid.NewGuid());
            var results = await Task.WhenAll(first, second);

            Assert.Single(results, result => result);
            Assert.Equal(5, await QueryScalarAsync<int>(
                database.ConnectionString,
                "SELECT TotalScore FROM dbo.RaceTeam;"));
            Assert.Equal(1, await QueryScalarAsync<int>(
                database.ConnectionString,
                "SELECT COUNT(1) FROM dbo.ScoringLog;"));
        }
        finally
        {
            await ExecuteSqlAsync(
                master.ConnectionString,
                $"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END;");
        }
    }

    private static async Task<bool> DebitAsync(
        string connectionString,
        Guid raceId,
        Guid teamId,
        Guid purchaseId)
    {
        var options = Options.Create(new DbConfigOptions
        {
            SqlServer = new DbConfigOptions.SqlServerOptions { ConnectionString = connectionString }
        });
        var factory = new SqlServerFactory(options);
        using var unitOfWork = new UnitOfWork(factory);
        var repository = new RaceRepository(new DapperExecutor(factory, unitOfWork));
        await unitOfWork.BeginAsync();
        try
        {
            var mutation = await repository.TryDebitRaceTeamScoreAsync(
                raceId, teamId, 15, teamId.ToString(), DateTime.UtcNow);
            if (mutation is null)
            {
                await unitOfWork.RollbackAsync();
                return false;
            }

            var now = DateTime.UtcNow;
            await repository.CreateScoringLogAsync(new ScoringLog
            {
                Id = Guid.NewGuid(),
                EventId = $"card-purchase:{raceId:N}:{purchaseId:N}",
                EventCode = "card-purchase",
                EventName = "Mua Data Patch",
                RaceId = raceId,
                TeamId = teamId,
                ActorId = teamId,
                Delta = -15,
                ScoreBefore = mutation.ScoreBefore,
                ScoreAfter = mutation.ScoreAfter,
                ReasonCode = "card_purchase",
                Reason = "Mua Data Patch test",
                CreatedAt = now,
                ModifiedAt = now,
                IsDeleted = false
            });
            await unitOfWork.CommitAsync();
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
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
                if (attempt < 30) await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
        throw new InvalidOperationException("SQL Server did not become ready within 60 seconds.", lastError);
    }

    private static async Task ExecuteSqlAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> QueryScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Expected SQL scalar result."));
    }

    private const string Schema = """
        CREATE TABLE dbo.RaceTeam
        (
            RaceID UNIQUEIDENTIFIER NOT NULL,
            TeamID UNIQUEIDENTIFIER NOT NULL,
            TotalScore INT NOT NULL,
            ModifiedBy NVARCHAR(100) NULL,
            ModifiedAt DATETIME2(7) NOT NULL,
            IsDeleted BIT NOT NULL,
            CONSTRAINT PK_RaceTeam PRIMARY KEY (RaceID, TeamID)
        );

        CREATE TABLE dbo.ScoringLog
        (
            Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            EventId NVARCHAR(200) NULL,
            EventCode NVARCHAR(100) NOT NULL,
            EventName NVARCHAR(255) NOT NULL,
            RaceId UNIQUEIDENTIFIER NOT NULL,
            TeamId UNIQUEIDENTIFIER NOT NULL,
            ActorId UNIQUEIDENTIFIER NULL,
            BoothId UNIQUEIDENTIFIER NULL,
            Delta INT NOT NULL,
            ScoreBefore INT NOT NULL,
            ScoreAfter INT NOT NULL,
            ReasonCode NVARCHAR(100) NOT NULL,
            Reason NVARCHAR(500) NOT NULL,
            CreatedBy NVARCHAR(100) NULL,
            CreatedAt DATETIME2(7) NOT NULL,
            ModifiedBy NVARCHAR(100) NULL,
            ModifiedAt DATETIME2(7) NOT NULL,
            IsDeleted BIT NOT NULL
        );

        CREATE UNIQUE INDEX UX_ScoringLog_Race_Event_Team_Code
            ON dbo.ScoringLog (RaceId, EventId, TeamId, EventCode)
            WHERE EventId IS NOT NULL AND IsDeleted = 0;
        """;
}
