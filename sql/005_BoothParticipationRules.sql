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

    DECLARE @ArchivedDuplicateCompletions TABLE
    (
        [RaceId] UNIQUEIDENTIFIER NOT NULL,
        [TeamId] UNIQUEIDENTIFIER NOT NULL,
        [Delta] INT NOT NULL
    );

    ;WITH RankedCompletions AS
    (
        SELECT
            [Id],
            ROW_NUMBER() OVER
            (
                PARTITION BY [RaceId], [TeamId], [BoothId]
                ORDER BY [CreatedAt], [Id]
            ) AS [CompletionNumber]
        FROM [dbo].[ScoringLog]
        WHERE [IsDeleted] = 0
          AND [BoothId] IS NOT NULL
          AND [ReasonCode] = N'BOOTH_COMPLETED'
    )
    UPDATE duplicateLog
    SET
        [IsDeleted] = 1,
        [ModifiedBy] = N'booth-participation-migration',
        [ModifiedAt] = SYSUTCDATETIME()
    OUTPUT
        inserted.[RaceId],
        inserted.[TeamId],
        inserted.[Delta]
    INTO @ArchivedDuplicateCompletions ([RaceId], [TeamId], [Delta])
    FROM [dbo].[ScoringLog] duplicateLog
    INNER JOIN RankedCompletions ranked
        ON ranked.[Id] = duplicateLog.[Id]
    WHERE ranked.[CompletionNumber] > 1;

    ;WITH ScoreAdjustments AS
    (
        SELECT
            [RaceId],
            [TeamId],
            SUM([Delta]) AS [DuplicateScore]
        FROM @ArchivedDuplicateCompletions
        GROUP BY [RaceId], [TeamId]
    )
    UPDATE raceTeam
    SET
        [TotalScore] = raceTeam.[TotalScore] - adjustment.[DuplicateScore],
        [ModifiedBy] = N'booth-participation-migration',
        [ModifiedAt] = SYSUTCDATETIME()
    FROM [dbo].[RaceTeam] raceTeam
    INNER JOIN ScoreAdjustments adjustment
        ON adjustment.[RaceId] = raceTeam.[RaceID]
       AND adjustment.[TeamId] = raceTeam.[TeamID]
    WHERE raceTeam.[IsDeleted] = 0;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'dbo.ScoringLog')
          AND [name] = N'UX_ScoringLog_CompletedBooth'
    )
    BEGIN
        CREATE UNIQUE INDEX [UX_ScoringLog_CompletedBooth]
            ON [dbo].[ScoringLog] ([RaceId], [TeamId], [BoothId])
            WHERE [IsDeleted] = 0
              AND [BoothId] IS NOT NULL
              AND [ReasonCode] = N'BOOTH_COMPLETED';
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'dbo.Booth')
          AND [name] = N'UX_Booth_OccupiedTeam'
    )
    BEGIN
        ;WITH RankedOccupiedBooths AS
        (
            SELECT
                [Id],
                ROW_NUMBER() OVER
                (
                    PARTITION BY [TeamId]
                    ORDER BY [ModifiedAt] DESC, [Id]
                ) AS [OccupancyNumber]
            FROM [dbo].[Booth]
            WHERE [IsDeleted] = 0
              AND [TeamId] IS NOT NULL
              AND [Status] = N'occupied'
        )
        UPDATE booth
        SET
            [Status] = N'free',
            [TeamId] = NULL,
            [ModifiedBy] = N'booth-participation-migration',
            [ModifiedAt] = SYSUTCDATETIME()
        FROM [dbo].[Booth] booth
        INNER JOIN RankedOccupiedBooths ranked
            ON ranked.[Id] = booth.[Id]
        WHERE ranked.[OccupancyNumber] > 1;

        CREATE UNIQUE INDEX [UX_Booth_OccupiedTeam]
            ON [dbo].[Booth] ([TeamId])
            WHERE [IsDeleted] = 0
              AND [TeamId] IS NOT NULL
              AND [Status] = N'occupied';
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'dbo.ScoringLog')
          AND [name] = N'IX_ScoringLog_TeamProgress'
    )
    BEGIN
        CREATE INDEX [IX_ScoringLog_TeamProgress]
            ON [dbo].[ScoringLog]
            (
                [RaceId],
                [TeamId],
                [ReasonCode],
                [IsDeleted],
                [BoothId]
            );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
