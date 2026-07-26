namespace OVCMOVE.Infrastructure.Helpers.QueriesHelper;

public static class RbacQueries
{
    public static string GetAllRolesQuery() => @"
        SELECT [Id], [Name], [Code], [Description], [IsSystem], [CreatedAt]
        FROM [dbo].[Roles] WITH (NOLOCK)
        WHERE [IsDeleted] = 0
        ORDER BY [Name];";

    public static string GetRoleByIdQuery() => @"
        SELECT [Id], [Name], [Code], [Description], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Roles] WITH (NOLOCK)
        WHERE [Id] = @RoleId AND [IsDeleted] = 0;";

    public static string GetRoleByCodeQuery() => @"
        SELECT [Id], [Name], [Code], [Description], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Roles] WITH (NOLOCK)
        WHERE [Code] = @Code AND [IsDeleted] = 0;";

    public static string CreateRoleQuery() => @"
        INSERT INTO [dbo].[Roles] ([Id], [Name], [Code], [Description], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES (@Id, @Name, @Code, @Description, @IsSystem, @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted);";

    public static string UpdateRoleQuery() => @"
        UPDATE [dbo].[Roles]
        SET [Name] = @Name,
            [Code] = @Code,
            [Description] = @Description,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string SoftDeleteRoleQuery() => @"
        UPDATE [dbo].[Roles]
        SET [IsDeleted] = 1,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @RoleId AND [IsDeleted] = 0;";

    public static string GetAllPermissionsQuery() => @"
        SELECT [Id], [Name], [Code], [Description], [Module], [Action], [IsSystem]
        FROM [dbo].[Permissions] WITH (NOLOCK)
        WHERE [IsDeleted] = 0
        ORDER BY [Module], [Action];";

    public static string GetPermissionByIdQuery() => @"
        SELECT [Id], [Name], [Code], [Description], [Module], [Action], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Permissions] WITH (NOLOCK)
        WHERE [Id] = @PermissionId AND [IsDeleted] = 0;";

    public static string GetPermissionByCodeQuery() => @"
        SELECT [Id], [Name], [Code], [Description], [Module], [Action], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Permissions] WITH (NOLOCK)
        WHERE [Code] = @Code AND [IsDeleted] = 0;";

    public static string CreatePermissionQuery() => @"
        INSERT INTO [dbo].[Permissions] ([Id], [Name], [Code], [Description], [Module], [Action], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES (@Id, @Name, @Code, @Description, @Module, @Action, @IsSystem, @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted);";

    public static string UpdatePermissionQuery() => @"
        UPDATE [dbo].[Permissions]
        SET [Name] = @Name,
            [Code] = @Code,
            [Description] = @Description,
            [Module] = @Module,
            [Action] = @Action,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string SoftDeletePermissionQuery() => @"
        UPDATE [dbo].[Permissions]
        SET [IsDeleted] = 1,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @PermissionId AND [IsDeleted] = 0;";

    public static string GetRoleIdsByUserIdQuery() => @"
        SELECT [RoleId]
        FROM [dbo].[UserRoles] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [IsDeleted] = 0;";

    public static string CreateUserRoleQuery() => @"
        INSERT INTO [dbo].[UserRoles] ([Id], [UserId], [RoleId], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES (@Id, @UserId, @RoleId, @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted);";

    public static string SoftDeleteUserRoleQuery() => @"
        UPDATE [dbo].[UserRoles]
        SET [IsDeleted] = 1,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [UserId] = @UserId AND [RoleId] = @RoleId AND [IsDeleted] = 0;";

    public static string GetPermissionIdsByRoleIdQuery() => @"
        SELECT [PermissionId]
        FROM [dbo].[RolePermissions] WITH (NOLOCK)
        WHERE [RoleId] = @RoleId AND [IsDeleted] = 0;";

    public static string CreateRolePermissionQuery() => @"
        INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES (@Id, @RoleId, @PermissionId, @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted);";

    public static string SoftDeleteRolePermissionQuery() => @"
        UPDATE [dbo].[RolePermissions]
        SET [IsDeleted] = 1,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [RoleId] = @RoleId AND [PermissionId] = @PermissionId AND [IsDeleted] = 0;";

    public static string GetAccessRolesByUserIdQuery() => @"
        SELECT DISTINCT r.[Id], r.[Name], r.[Code], r.[Description], r.[IsSystem]
        FROM [dbo].[UserRoles] ur WITH (NOLOCK)
        INNER JOIN [dbo].[Roles] r WITH (NOLOCK) ON r.[Id] = ur.[RoleId]
        WHERE ur.[UserId] = @UserId
          AND ur.[IsDeleted] = 0
          AND r.[IsDeleted] = 0
        ORDER BY r.[Name];";

    public static string GetAccessPermissionsByUserIdQuery() => @"
        SELECT DISTINCT p.[Id], p.[Name], p.[Code], p.[Description], p.[Module], p.[Action], p.[IsSystem]
        FROM [dbo].[UserRoles] ur WITH (NOLOCK)
        INNER JOIN [dbo].[Roles] r WITH (NOLOCK) ON r.[Id] = ur.[RoleId]
        INNER JOIN [dbo].[RolePermissions] rp WITH (NOLOCK) ON rp.[RoleId] = r.[Id]
        INNER JOIN [dbo].[Permissions] p WITH (NOLOCK) ON p.[Id] = rp.[PermissionId]
        WHERE ur.[UserId] = @UserId
          AND ur.[IsDeleted] = 0
          AND r.[IsDeleted] = 0
          AND rp.[IsDeleted] = 0
          AND p.[IsDeleted] = 0
        ORDER BY p.[Module], p.[Action];";
}
