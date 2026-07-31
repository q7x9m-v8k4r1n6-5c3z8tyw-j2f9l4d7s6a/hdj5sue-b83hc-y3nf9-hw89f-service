SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.Booth', N'TeamId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Booth]
        ADD [TeamId] UNIQUEIDENTIFIER NULL;
END;

IF COL_LENGTH(N'dbo.Booth', N'IsHidden') IS NULL
BEGIN
    ALTER TABLE [dbo].[Booth]
        ADD [IsHidden] BIT NOT NULL
            CONSTRAINT [DF_Booth_IsHidden] DEFAULT (0);
END;

IF COL_LENGTH(N'dbo.Booth', N'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[Booth]
        ADD [Status] NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_Booth_Status] DEFAULT (N'free');
END;

IF COL_LENGTH(N'dbo.RaceTeam', N'TotalScore') IS NULL
BEGIN
    ALTER TABLE [dbo].[RaceTeam]
        ADD [TotalScore] INT NOT NULL
            CONSTRAINT [DF_RaceTeam_TotalScore] DEFAULT (0);
END;

IF OBJECT_ID(N'dbo.ScoringLog', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ScoringLog]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_ScoringLog] PRIMARY KEY
            CONSTRAINT [DF_ScoringLog_Id] DEFAULT (NEWID()),
        [EventCode] NVARCHAR(100) NOT NULL
            CONSTRAINT [DF_ScoringLog_EventCode] DEFAULT (N''),
        [EventName] NVARCHAR(255) NOT NULL
            CONSTRAINT [DF_ScoringLog_EventName] DEFAULT (N''),
        [RaceId] UNIQUEIDENTIFIER NOT NULL,
        [TeamId] UNIQUEIDENTIFIER NOT NULL,
        [ActorId] UNIQUEIDENTIFIER NULL,
        [BoothId] UNIQUEIDENTIFIER NULL,
        [Delta] INT NOT NULL,
        [ScoreBefore] INT NOT NULL,
        [ScoreAfter] INT NOT NULL,
        [ReasonCode] NVARCHAR(100) NOT NULL
            CONSTRAINT [DF_ScoringLog_ReasonCode] DEFAULT (N''),
        [Reason] NVARCHAR(500) NOT NULL
            CONSTRAINT [DF_ScoringLog_Reason] DEFAULT (N''),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_ScoringLog_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_ScoringLog_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_ScoringLog_IsDeleted] DEFAULT (0)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_ScoringLog_RaceId_CreatedAt'
      AND [object_id] = OBJECT_ID(N'dbo.ScoringLog')
)
BEGIN
    CREATE INDEX [IX_ScoringLog_RaceId_CreatedAt]
        ON [dbo].[ScoringLog] ([RaceId], [CreatedAt] DESC);
END;
