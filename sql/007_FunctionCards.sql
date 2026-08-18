SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[dbo].[FunctionCards]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[FunctionCards]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_FunctionCards] PRIMARY KEY,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [TeamId] UNIQUEIDENTIFIER NULL,
            [CardKey] NVARCHAR(100) NOT NULL,
            [Name] NVARCHAR(255) NOT NULL,
            [Description] NVARCHAR(1000) NOT NULL CONSTRAINT [DF_FunctionCards_Description] DEFAULT (N''),
            [Category] NVARCHAR(30) NOT NULL,
            [BackgroundUrl] NVARCHAR(2048) NULL,
            [InputsJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_FunctionCards_InputsJson] DEFAULT (N'[]'),
            [CreatedBy] NVARCHAR(100) NULL,
            [CreatedAt] DATETIME2(7) NOT NULL,
            [ModifiedBy] NVARCHAR(100) NULL,
            [ModifiedAt] DATETIME2(7) NOT NULL,
            [IsDeleted] BIT NOT NULL CONSTRAINT [DF_FunctionCards_IsDeleted] DEFAULT (0),
            CONSTRAINT [CK_FunctionCards_Category] CHECK ([Category] IN (N'attack', N'defense', N'effect')),
            CONSTRAINT [CK_FunctionCards_InputsJson] CHECK (ISJSON([InputsJson]) = 1 AND LEFT(LTRIM([InputsJson]), 1) = N'['),
            CONSTRAINT [FK_FunctionCards_Race] FOREIGN KEY ([RaceId]) REFERENCES [dbo].[Race] ([Id]),
            CONSTRAINT [FK_FunctionCards_Team] FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Users] ([Id])
        );

        CREATE UNIQUE INDEX [UX_FunctionCards_Race_CardKey]
            ON [dbo].[FunctionCards] ([RaceId], [CardKey])
            WHERE [IsDeleted] = 0;
        CREATE INDEX [IX_FunctionCards_Race_Team]
            ON [dbo].[FunctionCards] ([RaceId], [TeamId])
            WHERE [IsDeleted] = 0;
    END;

    /* Preserve workflows created before cards became backend entities. */
    INSERT INTO [dbo].[FunctionCards]
    ([Id], [RaceId], [TeamId], [CardKey], [Name], [Description], [Category],
     [BackgroundUrl], [InputsJson], [CreatedBy], [CreatedAt], [ModifiedBy],
     [ModifiedAt], [IsDeleted])
    SELECT NEWID(), W.[RaceId], NULL, W.[CardKey], MAX(W.[CardName]), N'',
           CASE WHEN MAX(CASE WHEN W.[TriggerType] = N'attacked' THEN 1 ELSE 0 END) = 1
                THEN N'defense' ELSE N'effect' END,
           NULL, N'[]', MIN(W.[CreatedBy]), MIN(W.[CreatedAt]),
           MAX(W.[ModifiedBy]), MAX(W.[ModifiedAt]), 0
    FROM [dbo].[Workflows] W
    WHERE W.[IsDeleted] = 0
      AND NOT EXISTS
      (
          SELECT 1 FROM [dbo].[FunctionCards] FC
          WHERE FC.[RaceId] = W.[RaceId]
            AND FC.[CardKey] = W.[CardKey]
            AND FC.[IsDeleted] = 0
      )
    GROUP BY W.[RaceId], W.[CardKey];

    IF COL_LENGTH(N'[dbo].[Workflows]', N'CardId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Workflows] ADD [CardId] UNIQUEIDENTIFIER NULL;
    END;

    EXEC sys.sp_executesql N'
        UPDATE W
        SET W.[CardId] = FC.[Id]
        FROM [dbo].[Workflows] W
        INNER JOIN [dbo].[FunctionCards] FC
            ON FC.[RaceId] = W.[RaceId]
           AND FC.[CardKey] = W.[CardKey]
           AND FC.[IsDeleted] = 0
        WHERE W.[CardId] IS NULL;

        IF EXISTS (SELECT 1 FROM [dbo].[Workflows] WHERE [CardId] IS NULL)
            THROW 51000, ''Unable to connect every workflow to a function card.'', 1;

        ALTER TABLE [dbo].[Workflows]
            ALTER COLUMN [CardId] UNIQUEIDENTIFIER NOT NULL;
    ';

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[dbo].[Workflows]')
          AND [name] IN (N'UX_Workflows_Race_Card', N'UX_Workflows_Race_Card_Trigger'))
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[Workflows]') AND [name] = N'UX_Workflows_Race_Card')
            DROP INDEX [UX_Workflows_Race_Card] ON [dbo].[Workflows];
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[Workflows]') AND [name] = N'UX_Workflows_Race_Card_Trigger')
            DROP INDEX [UX_Workflows_Race_Card_Trigger] ON [dbo].[Workflows];
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[dbo].[Workflows]')
          AND [name] = N'UX_Workflows_CardId')
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE UNIQUE INDEX [UX_Workflows_CardId]
                ON [dbo].[Workflows] ([CardId])
                WHERE [IsDeleted] = 0;
        ';
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Workflows_FunctionCard')
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE [dbo].[Workflows] WITH CHECK
                ADD CONSTRAINT [FK_Workflows_FunctionCard]
                FOREIGN KEY ([CardId]) REFERENCES [dbo].[FunctionCards] ([Id]);
        ';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
