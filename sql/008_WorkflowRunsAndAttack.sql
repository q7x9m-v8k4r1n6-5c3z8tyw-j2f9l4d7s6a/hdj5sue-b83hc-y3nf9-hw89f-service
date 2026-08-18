SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[dbo].[WorkflowRuns]', N'U') IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM sys.check_constraints
            WHERE [parent_object_id] = OBJECT_ID(N'[dbo].[WorkflowRuns]')
              AND [name] = N'CK_WorkflowRuns_Status')
        BEGIN
            ALTER TABLE [dbo].[WorkflowRuns]
                DROP CONSTRAINT [CK_WorkflowRuns_Status];
        END;

        ALTER TABLE [dbo].[WorkflowRuns] WITH CHECK
            ADD CONSTRAINT [CK_WorkflowRuns_Status]
            CHECK ([Status] IN (N'running', N'succeeded', N'failed', N'canceled'));
    END;

    IF OBJECT_ID(N'[dbo].[Workflows]', N'U') IS NOT NULL
    BEGIN
        UPDATE [dbo].[Workflows]
        SET [Status] = N'published',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Status] = N'draft'
          AND [IsDeleted] = 0;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
