namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class TeamQueries
{
    public static string GetExistingIdsQuery() => @"
        SELECT [Id]
        FROM [dbo].[Users]
        WHERE [Id] IN @Ids
          AND [UserType] = @UserType
          AND [IsDeleted] = 0;";

    public static string GetAllTeamsQuery() => @"
        SELECT
            [Id], [Username], [LinkedEmail], [UserType],
            [DisplayName], [ShortName], [Status],
            [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Users]
        WHERE [UserType] = @UserType
          AND [IsDeleted] = 0
        ORDER BY COALESCE(NULLIF([DisplayName], N''), [Username], [LinkedEmail])
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public static string CountTeamsQuery() => @"
        SELECT COUNT(1)
        FROM [dbo].[Users]
        WHERE [UserType] = @UserType
          AND [IsDeleted] = 0;";

    public static string SearchTeamQuery() => @"
        SELECT TOP (20)
            [Id], [Username], [LinkedEmail], [UserType],
            [DisplayName], [ShortName], [Status],
            [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Users]
        WHERE [UserType] = @UserType
          AND [IsDeleted] = 0
          AND (
              COALESCE(NULLIF([DisplayName], N''), [Username], [LinkedEmail]) LIKE @Keyword
              OR [Username] LIKE @Keyword
              OR [LinkedEmail] LIKE @Keyword
          )
        ORDER BY COALESCE(NULLIF([DisplayName], N''), [Username], [LinkedEmail]);";
}
