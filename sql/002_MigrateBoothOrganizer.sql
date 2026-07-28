/*
    Non-destructive upgrade for databases that still store organizer IDs in
    dbo.Booth.BoothOrganizerID as comma-separated text.

    Run this script before deploying the application version that reads
    dbo.BoothOrganizer. It is safe to rerun. The legacy column is intentionally
    retained for one rollback window; the application no longer reads or writes it.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[dbo].[BoothOrganizer]', N'U') IS NULL
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE TABLE [dbo].[BoothOrganizer]
            (
                [Id] UNIQUEIDENTIFIER NOT NULL
                    CONSTRAINT [PK_BoothOrganizer] PRIMARY KEY
                    CONSTRAINT [DF_BoothOrganizer_Id] DEFAULT (NEWID()),
                [BoothId] UNIQUEIDENTIFIER NOT NULL,
                [OrganizerId] UNIQUEIDENTIFIER NOT NULL,
                [CreatedBy] NVARCHAR(100) NULL,
                [CreatedAt] DATETIME2(7) NOT NULL
                    CONSTRAINT [DF_BoothOrganizer_CreatedAt] DEFAULT (SYSUTCDATETIME()),
                [ModifiedBy] NVARCHAR(100) NULL,
                [ModifiedAt] DATETIME2(7) NOT NULL
                    CONSTRAINT [DF_BoothOrganizer_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
                [IsDeleted] BIT NOT NULL
                    CONSTRAINT [DF_BoothOrganizer_IsDeleted] DEFAULT (0)
            );
        ';
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[dbo].[BoothOrganizer]')
          AND [name] = N'UX_BoothOrganizer_BoothId_OrganizerId'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE UNIQUE INDEX [UX_BoothOrganizer_BoothId_OrganizerId]
                ON [dbo].[BoothOrganizer] ([BoothId], [OrganizerId])
                WHERE [IsDeleted] = 0;
        ';
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[dbo].[BoothOrganizer]')
          AND [name] = N'IX_BoothOrganizer_OrganizerId'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX [IX_BoothOrganizer_OrganizerId]
                ON [dbo].[BoothOrganizer] ([OrganizerId])
                WHERE [IsDeleted] = 0;
        ';
    END;

    -- Dynamic SQL keeps this script compatible with new databases where the
    -- legacy column no longer exists.
    IF COL_LENGTH(N'dbo.Booth', N'BoothOrganizerID') IS NOT NULL
    BEGIN
        EXEC sys.sp_executesql N'
            INSERT INTO [dbo].[BoothOrganizer]
            (
                [Id],
                [BoothId],
                [OrganizerId],
                [CreatedBy],
                [CreatedAt],
                [ModifiedBy],
                [ModifiedAt],
                [IsDeleted]
            )
            SELECT
                NEWID(),
                source.[BoothId],
                source.[OrganizerId],
                N''booth-organizer-migration'',
                SYSUTCDATETIME(),
                N''booth-organizer-migration'',
                SYSUTCDATETIME(),
                0
            FROM
            (
                SELECT DISTINCT
                    booth.[Id] AS [BoothId],
                    TRY_CONVERT(UNIQUEIDENTIFIER, LTRIM(RTRIM(value.[value]))) AS [OrganizerId]
                FROM [dbo].[Booth] AS booth
                CROSS APPLY STRING_SPLIT(booth.[BoothOrganizerID], N'','') AS value
                WHERE booth.[IsDeleted] = 0
            ) AS source
            WHERE source.[OrganizerId] IS NOT NULL
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM [dbo].[BoothOrganizer] AS existing
                  WHERE existing.[BoothId] = source.[BoothId]
                    AND existing.[OrganizerId] = source.[OrganizerId]
                    AND existing.[IsDeleted] = 0
              );
        ';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
