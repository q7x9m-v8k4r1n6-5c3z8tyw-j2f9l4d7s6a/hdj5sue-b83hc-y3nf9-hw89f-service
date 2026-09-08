SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.ScoringLog', N'EventId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ScoringLog]
            ADD [EventId] NVARCHAR(200) NULL;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'dbo.ScoringLog')
          AND [name] = N'UX_ScoringLog_Race_Event_Team_Code'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE UNIQUE INDEX [UX_ScoringLog_Race_Event_Team_Code]
                ON [dbo].[ScoringLog]
                    ([RaceId], [EventId], [TeamId], [EventCode])
                WHERE [EventId] IS NOT NULL AND [IsDeleted] = 0;
        ';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
