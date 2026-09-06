SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.Booth', N'Type') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Booth]
            ADD [Type] NVARCHAR(30) NOT NULL
                CONSTRAINT [DF_Booth_Type] DEFAULT (N'other');
    END;

    IF COL_LENGTH(N'dbo.Booth', N'MaximumScore') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Booth]
            ADD [MaximumScore] INT NULL;
    END;

    -- Type may have been added earlier in this same batch. Dynamic SQL avoids
    -- SQL Server resolving the new column before ALTER TABLE has executed.
    EXEC sys.sp_executesql N'
        UPDATE [dbo].[Booth]
        SET [Type] = N''other''
        WHERE [Type] IS NULL
           OR LTRIM(RTRIM([Type])) NOT IN
              (N''other'', N''intellectual'', N''physical'');
    ';

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE [parent_object_id] = OBJECT_ID(N'dbo.Booth')
          AND [name] = N'CK_Booth_Type'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE [dbo].[Booth] WITH CHECK
                ADD CONSTRAINT [CK_Booth_Type]
                    CHECK ([Type] IN
                        (N''other'', N''intellectual'', N''physical''));
        ';
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE [parent_object_id] = OBJECT_ID(N'dbo.Booth')
          AND [name] = N'CK_Booth_MaximumScore'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE [dbo].[Booth] WITH CHECK
                ADD CONSTRAINT [CK_Booth_MaximumScore]
                    CHECK ([MaximumScore] IS NULL OR [MaximumScore] BETWEEN 0 AND 100);
        ';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
