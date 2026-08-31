/*
    Removes the SQL-backed card/workflow feature.

    Card state now belongs to the optional plugin and is stored in MongoDB.
    This migration is idempotent so existing environments can run it safely.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @DropConstraints NVARCHAR(MAX) = N'';

    SELECT @DropConstraints +=
        N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) +
        N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) +
        N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id IN
    (
        OBJECT_ID(N'[dbo].[WorkflowRuns]', N'U'),
        OBJECT_ID(N'[dbo].[Workflows]', N'U'),
        OBJECT_ID(N'[dbo].[FunctionCards]', N'U')
    )
    OR fk.referenced_object_id IN
    (
        OBJECT_ID(N'[dbo].[WorkflowRuns]', N'U'),
        OBJECT_ID(N'[dbo].[Workflows]', N'U'),
        OBJECT_ID(N'[dbo].[FunctionCards]', N'U')
    );

    IF @DropConstraints <> N''
        EXEC sys.sp_executesql @DropConstraints;

    IF OBJECT_ID(N'[dbo].[WorkflowRuns]', N'U') IS NOT NULL
        DROP TABLE [dbo].[WorkflowRuns];

    IF OBJECT_ID(N'[dbo].[Workflows]', N'U') IS NOT NULL
        DROP TABLE [dbo].[Workflows];

    IF OBJECT_ID(N'[dbo].[FunctionCards]', N'U') IS NOT NULL
        DROP TABLE [dbo].[FunctionCards];

    IF OBJECT_ID(N'[dbo].[Permissions]', N'U') IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'[dbo].[RolePermissions]', N'U') IS NOT NULL
        BEGIN
            DELETE rolePermission
            FROM [dbo].[RolePermissions] rolePermission
            INNER JOIN [dbo].[Permissions] permission
                ON permission.[Id] = rolePermission.[PermissionId]
            WHERE permission.[Code] = N'workflow.manage';
        END;

        DELETE FROM [dbo].[Permissions]
        WHERE [Code] = N'workflow.manage';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
