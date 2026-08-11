SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.BoothOrganizer', N'RaceId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[BoothOrganizer]
            ADD [RaceId] UNIQUEIDENTIFIER NULL;
    END;

    EXEC sys.sp_executesql N'
        UPDATE assignment
        SET [RaceId] = booth.[RaceID],
            [ModifiedBy] = N''durable-booth-session-migration'',
            [ModifiedAt] = SYSUTCDATETIME()
        FROM [dbo].[BoothOrganizer] assignment
        INNER JOIN [dbo].[Booth] booth
            ON booth.[Id] = assignment.[BoothId]
        WHERE assignment.[RaceId] IS NULL;

        IF EXISTS
        (
            SELECT 1
            FROM [dbo].[BoothOrganizer]
            WHERE [RaceId] IS NULL
        )
            THROW 51000, ''BoothOrganizer rows could not be assigned to a race.'', 1;

        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N''dbo.BoothOrganizer'')
              AND [name] = N''UX_BoothOrganizer_RaceId_OrganizerId''
        )
            DROP INDEX [UX_BoothOrganizer_RaceId_OrganizerId]
                ON [dbo].[BoothOrganizer];

        ALTER TABLE [dbo].[BoothOrganizer]
            ALTER COLUMN [RaceId] UNIQUEIDENTIFIER NOT NULL;

        ;WITH RankedOrganizerAssignments AS
        (
            SELECT
                [Id],
                ROW_NUMBER() OVER
                (
                    PARTITION BY [RaceId], [OrganizerId]
                    ORDER BY [CreatedAt], [Id]
                ) AS [AssignmentNumber]
            FROM [dbo].[BoothOrganizer]
            WHERE [IsDeleted] = 0
              AND [RaceId] IS NOT NULL
        )
        UPDATE assignment
        SET [IsDeleted] = 1,
            [ModifiedBy] = N''durable-booth-session-migration'',
            [ModifiedAt] = SYSUTCDATETIME()
        FROM [dbo].[BoothOrganizer] assignment
        INNER JOIN RankedOrganizerAssignments ranked
            ON ranked.[Id] = assignment.[Id]
        WHERE ranked.[AssignmentNumber] > 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N''dbo.BoothOrganizer'')
              AND [name] = N''UX_BoothOrganizer_RaceId_OrganizerId''
        )
        BEGIN
            CREATE UNIQUE INDEX [UX_BoothOrganizer_RaceId_OrganizerId]
                ON [dbo].[BoothOrganizer] ([RaceId], [OrganizerId])
                WHERE [IsDeleted] = 0
                  AND [RaceId] IS NOT NULL;
        END;
    ';

    UPDATE [dbo].[Booth]
    SET [Status] = N'free',
        [ModifiedBy] = N'durable-booth-session-migration',
        [ModifiedAt] = SYSUTCDATETIME()
    WHERE [IsDeleted] = 0
      AND [TeamId] IS NULL
      AND [Status] <> N'free';

    UPDATE [dbo].[Booth]
    SET [Status] = N'free',
        [TeamId] = NULL,
        [ModifiedBy] = N'durable-booth-session-migration',
        [ModifiedAt] = SYSUTCDATETIME()
    WHERE [IsDeleted] = 0
      AND [TeamId] IS NOT NULL
      AND [Status] NOT IN (N'pending', N'occupied');

    ;WITH RankedActiveTeams AS
    (
        SELECT
            [Id],
            ROW_NUMBER() OVER
            (
                PARTITION BY [TeamId]
                ORDER BY [ModifiedAt] DESC, [Id]
            ) AS [ActiveNumber]
        FROM [dbo].[Booth]
        WHERE [IsDeleted] = 0
          AND [TeamId] IS NOT NULL
          AND [Status] IN (N'pending', N'occupied')
    )
    UPDATE booth
    SET [Status] = N'free',
        [TeamId] = NULL,
        [ModifiedBy] = N'durable-booth-session-migration',
        [ModifiedAt] = SYSUTCDATETIME()
    FROM [dbo].[Booth] booth
    INNER JOIN RankedActiveTeams ranked
        ON ranked.[Id] = booth.[Id]
    WHERE ranked.[ActiveNumber] > 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'dbo.Booth')
          AND [name] = N'UX_Booth_OccupiedTeam'
    )
        DROP INDEX [UX_Booth_OccupiedTeam] ON [dbo].[Booth];

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'dbo.Booth')
          AND [name] = N'UX_Booth_ActiveTeam'
    )
    BEGIN
        CREATE UNIQUE INDEX [UX_Booth_ActiveTeam]
            ON [dbo].[Booth] ([TeamId])
            WHERE [IsDeleted] = 0
              AND [TeamId] IS NOT NULL;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
