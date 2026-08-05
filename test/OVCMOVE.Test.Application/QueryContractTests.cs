using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Test.Application;

public class QueryContractTests
{
    [Fact]
    public void RaceUpdate_UsesOptimisticConcurrencyToken()
    {
        var sql = RaceQueries.UpdateRaceQuery();

        Assert.Contains("[ModifiedAt] = @ExpectedModifiedAt", sql);
    }

    [Fact]
    public void TeamPaging_IsExecutedBySqlServer()
    {
        var sql = TeamQueries.GetAllTeamsQuery();

        Assert.Contains("OFFSET @Offset ROWS", sql);
        Assert.Contains("FETCH NEXT @PageSize ROWS ONLY", sql);
    }

    [Fact]
    public void RacePaging_IsExecutedBySqlServer()
    {
        var sql = RaceQueries.GetAllRacesQuery();

        Assert.Contains("OFFSET @Offset ROWS", sql);
        Assert.Contains("FETCH NEXT @PageSize ROWS ONLY", sql);
    }

    [Fact]
    public void OrganizerPaging_IsExecutedBySqlServer()
    {
        var sql = OrganizerQueries.GetAllOrganizersQuery();

        Assert.Contains("OFFSET @Offset ROWS", sql);
        Assert.Contains("FETCH NEXT @PageSize ROWS ONLY", sql);
        Assert.Contains("[UserRoles]", sql);
        Assert.Contains("[Roles]", sql);
    }

    [Fact]
    public void LookupSearches_AreBounded()
    {
        Assert.Contains("TOP (20)", TeamQueries.SearchTeamQuery());
        Assert.Contains("TOP (20)", OrganizerQueries.SearchOrganizerQuery());
    }

    [Fact]
    public void PublicUserLookups_DoNotLoadPasswordHashes()
    {
        Assert.DoesNotContain(
            "PasswordHash",
            TeamQueries.GetAllTeamsQuery(),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "PasswordHash",
            TeamQueries.SearchTeamQuery(),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "PasswordHash",
            OrganizerQueries.GetAllOrganizersQuery(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthenticationQueries_ExcludeSoftDeletedRecords()
    {
        Assert.Contains("[IsDeleted] = 0", UserQueries.GetByUsernameQuery());
        Assert.Contains(
            "[IsDeleted] = 0",
            RefreshTokenQueries.GetByTokenHashQuery());
    }

    [Fact]
    public void RefreshTokenWrites_DoNotPersistTheRawToken()
    {
        var sql = RefreshTokenQueries.CreateQuery();

        Assert.DoesNotContain(
            "FamilyId, Token, TokenHash",
            sql,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BoothOrganizers_UseARelationshipTableInsteadOfCsvParsing()
    {
        var sql = RaceQueries.GetRaceOrganizerDetailsQuery();

        Assert.Contains("[BoothOrganizer]", sql);
        Assert.DoesNotContain(
            "STRING_SPLIT",
            sql,
            StringComparison.OrdinalIgnoreCase);
    }

}
