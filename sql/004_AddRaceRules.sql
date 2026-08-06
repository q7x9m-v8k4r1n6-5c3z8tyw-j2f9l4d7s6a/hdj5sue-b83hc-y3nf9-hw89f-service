SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.Race', N'Rules') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Race]
            ADD [Rules] NVARCHAR(MAX) NOT NULL
                CONSTRAINT [DF_Race_Rules] DEFAULT (N'');
    END;

    IF COL_LENGTH(N'dbo.Race', N'Rules') IS NOT NULL
    BEGIN
        UPDATE [dbo].[Race]
        SET [Rules] = N''
        WHERE [Rules] IS NULL;

        ALTER TABLE [dbo].[Race]
            ALTER COLUMN [Rules] NVARCHAR(MAX) NOT NULL;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns columnInfo
                ON columnInfo.[object_id] = dc.[parent_object_id]
               AND columnInfo.[column_id] = dc.[parent_column_id]
            WHERE dc.[parent_object_id] = OBJECT_ID(N'dbo.Race')
              AND columnInfo.[name] = N'Rules'
        )
        BEGIN
            ALTER TABLE [dbo].[Race]
                ADD CONSTRAINT [DF_Race_Rules]
                    DEFAULT (N'') FOR [Rules];
        END;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
