using System.Reflection;
using OVCMOVE.Api.Security;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Test.Application;

public class DatabaseScriptContractTests
{
    [Fact]
    public void SeedScript_ContainsEveryApiPermissionCode()
    {
        var repositoryRoot = FindRepositoryRoot();
        var seedScript = File.ReadAllText(
            Path.Combine(repositoryRoot, "sql", "001_SeedDatabase.sql"));
        var permissionCodes = typeof(PermissionCodes)
            .GetFields(BindingFlags.Public |
                       BindingFlags.Static |
                       BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => Assert.IsType<string>(
                field.GetRawConstantValue()));

        foreach (var permissionCode in permissionCodes)
        {
            Assert.Contains(
                $"N'{permissionCode}'",
                seedScript,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ResetSchema_NormalizesBoothOrganizerRelationships()
    {
        var repositoryRoot = FindRepositoryRoot();
        var resetScript = File.ReadAllText(
            Path.Combine(repositoryRoot, "sql", "000_ResetDatabase.sql"));

        Assert.Contains(
            "CREATE TABLE [dbo].[BoothOrganizer]",
            resetScript,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "[BoothOrganizerID]",
            resetScript,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BoothOrganizerMigration_IsRerunnableAndKeepsRollbackColumn()
    {
        var repositoryRoot = FindRepositoryRoot();
        var migrationScript = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "sql",
                "002_MigrateBoothOrganizer.sql"));

        Assert.Contains(
            "IF OBJECT_ID(N'[dbo].[BoothOrganizer]', N'U') IS NULL",
            migrationScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "AND NOT EXISTS",
            migrationScript,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "DROP COLUMN",
            migrationScript,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RaceRulesMigration_BackfillsAndMakesRulesRequired()
    {
        var repositoryRoot = FindRepositoryRoot();
        var migrationScript = File.ReadAllText(
            Path.Combine(repositoryRoot, "sql", "004_AddRaceRules.sql"));
        var resetScript = File.ReadAllText(
            Path.Combine(repositoryRoot, "sql", "000_ResetDatabase.sql"));

        Assert.Contains("WHERE [Rules] IS NULL", migrationScript);
        Assert.Contains("ALTER COLUMN [Rules] NVARCHAR(MAX) NOT NULL", migrationScript);
        Assert.Contains("[Rules] NVARCHAR(MAX) NOT NULL", resetScript);
    }

    [Fact]
    public void BoothParticipationSchema_UsesCanonicalCodeAndGuardsOccupancy()
    {
        var repositoryRoot = FindRepositoryRoot();
        var migrationScript = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "sql",
                "005_BoothParticipationRules.sql"));
        var resetScript = File.ReadAllText(
            Path.Combine(repositoryRoot, "sql", "000_ResetDatabase.sql"));

        Assert.Equal(
            "BOOTH_COMPLETED",
            ScoringLogConstants.ReasonCode.BoothCompleted);
        Assert.Contains(
            "[ReasonCode] = N'BOOTH_COMPLETED'",
            migrationScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "UX_Booth_OccupiedTeam",
            migrationScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "UX_Booth_OccupiedTeam",
            resetScript,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(
                   directory.FullName,
                   "OVCMOVE.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException(
                "Could not locate the repository root.");
    }
}
