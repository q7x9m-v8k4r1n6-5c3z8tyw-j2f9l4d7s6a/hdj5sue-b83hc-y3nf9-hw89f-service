namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class OrganizerQueries
{
    private const string UserColumns = @"
        [Id], [Username], [LinkedEmail], [UserType],
        [DisplayName], [AvatarUrl], [ShortName], [Status],
        [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]";

    public static string GetExistingIdsQuery() => @"
        SELECT [Id]
        FROM [dbo].[Users]
        WHERE [Id] IN @Ids
          AND [UserType] = @UserType
          AND [IsDeleted] = 0;";

    public static string GetByEmailQuery() => $@"
        SELECT {UserColumns}
        FROM [dbo].[Users]
        WHERE [LinkedEmail] = @LinkedEmail
          AND [UserType] = @UserType
          AND [IsDeleted] = 0;";

    public static string GetAllOrganizersQuery() => @"
        SELECT
            U.[Id],
            U.[Id] AS [UserId],
            COALESCE(NULLIF(U.[DisplayName], N''), U.[LinkedEmail]) AS [DisplayName],
            U.[LinkedEmail] AS [Email],
            U.[AvatarUrl],
            COALESCE(PrimaryRole.[Code], U.[UserType]) AS [Role],
            U.[Status]
        FROM [dbo].[Users] U
        OUTER APPLY
        (
            SELECT TOP (1) R.[Code]
            FROM [dbo].[UserRoles] UR
            INNER JOIN [dbo].[Roles] R
                ON R.[Id] = UR.[RoleId]
               AND R.[IsDeleted] = 0
            WHERE UR.[UserId] = U.[Id]
              AND UR.[IsDeleted] = 0
            ORDER BY
                CASE R.[Code]
                    WHEN N'admin' THEN 1
                    WHEN N'organizer' THEN 2
                    ELSE 3
                END,
                R.[Code]
        ) PrimaryRole
        WHERE U.[UserType] = @UserType
          AND U.[IsDeleted] = 0
          AND (
              @Search IS NULL
              OR U.[DisplayName] LIKE N'%' + @Search + N'%'
              OR U.[LinkedEmail] LIKE N'%' + @Search + N'%'
          )
        ORDER BY COALESCE(NULLIF(U.[DisplayName], N''), U.[LinkedEmail])
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

    public static string CountOrganizersQuery() => @"
        SELECT COUNT(1)
        FROM [dbo].[Users]
        WHERE [UserType] = @UserType
          AND [IsDeleted] = 0
          AND (
              @Search IS NULL
              OR [DisplayName] LIKE N'%' + @Search + N'%'
              OR [LinkedEmail] LIKE N'%' + @Search + N'%'
          );";

    public static string SearchOrganizerQuery() => $@"
        SELECT TOP (20) {UserColumns}
        FROM [dbo].[Users]
        WHERE [UserType] = @UserType
          AND [IsDeleted] = 0
          AND (
              [DisplayName] LIKE @Keyword
              OR [LinkedEmail] LIKE @Keyword
          )
        ORDER BY COALESCE(NULLIF([DisplayName], N''), [LinkedEmail]);";

    public static string GetOrganizerByIdQuery() => $@"
        SELECT {UserColumns}
        FROM [dbo].[Users]
        WHERE [Id] = @OrganizerId
          AND [UserType] = @UserType
          AND [IsDeleted] = 0;";

    public static string UpdateOrganizerStatusQuery() => @"
        UPDATE [dbo].[Users]
        SET [Status] = @UserStatus,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @OrganizerId
          AND [UserType] = @UserType
          AND [IsDeleted] = 0;";

    public static string UpdateOrganizerQuery() => @"
        UPDATE [dbo].[Users]
        SET [DisplayName] = @DisplayName, [Status] = @Status,
            [ModifiedBy] = @ModifiedBy, [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id AND [UserType] = @UserType AND [IsDeleted] = 0;";
}
