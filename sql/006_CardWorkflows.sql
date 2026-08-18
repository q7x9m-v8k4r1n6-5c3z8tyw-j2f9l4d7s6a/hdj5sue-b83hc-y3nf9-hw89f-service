SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[dbo].[Workflows]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[Workflows]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Workflows] PRIMARY KEY,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [CardKey] NVARCHAR(100) NOT NULL,
            [CardName] NVARCHAR(255) NOT NULL,
            [Name] NVARCHAR(255) NOT NULL,
            [Description] NVARCHAR(1000) NOT NULL CONSTRAINT [DF_Workflows_Description] DEFAULT (N''),
            [TriggerType] NVARCHAR(30) NOT NULL,
            [Status] NVARCHAR(30) NOT NULL,
            [Version] INT NOT NULL CONSTRAINT [DF_Workflows_Version] DEFAULT (1),
            [DefinitionJson] NVARCHAR(MAX) NOT NULL,
            [CreatedBy] NVARCHAR(100) NULL,
            [CreatedAt] DATETIME2(7) NOT NULL,
            [ModifiedBy] NVARCHAR(100) NULL,
            [ModifiedAt] DATETIME2(7) NOT NULL,
            [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Workflows_IsDeleted] DEFAULT (0),
            CONSTRAINT [CK_Workflows_TriggerType] CHECK ([TriggerType] IN (N'activated', N'attacked')),
            CONSTRAINT [CK_Workflows_Status] CHECK ([Status] IN (N'draft', N'published', N'disabled')),
            CONSTRAINT [CK_Workflows_DefinitionJson] CHECK (ISJSON([DefinitionJson]) = 1),
            CONSTRAINT [FK_Workflows_Race] FOREIGN KEY ([RaceId]) REFERENCES [dbo].[Race] ([Id])
        );

        CREATE INDEX [IX_Workflows_Race_Status]
            ON [dbo].[Workflows] ([RaceId], [Status], [ModifiedAt] DESC)
            WHERE [IsDeleted] = 0;
    END;

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[dbo].[Workflows]')
          AND [name] = N'UX_Workflows_Race_Card_Trigger')
    BEGIN
        DROP INDEX [UX_Workflows_Race_Card_Trigger] ON [dbo].[Workflows];
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[dbo].[Workflows]')
          AND [name] = N'UX_Workflows_Race_Card')
    BEGIN
        CREATE UNIQUE INDEX [UX_Workflows_Race_Card]
            ON [dbo].[Workflows] ([RaceId], [CardKey])
            WHERE [IsDeleted] = 0;
    END;

    IF OBJECT_ID(N'[dbo].[WorkflowRuns]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[WorkflowRuns]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_WorkflowRuns] PRIMARY KEY,
            [WorkflowId] UNIQUEIDENTIFIER NOT NULL,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [CardKey] NVARCHAR(100) NOT NULL,
            [TriggerType] NVARCHAR(30) NOT NULL,
            [EventId] NVARCHAR(100) NULL,
            [Status] NVARCHAR(30) NOT NULL,
            [IsSimulation] BIT NOT NULL,
            [InputJson] NVARCHAR(MAX) NOT NULL,
            [OutputJson] NVARCHAR(MAX) NOT NULL,
            [Error] NVARCHAR(2000) NULL,
            [StartedAt] DATETIME2(7) NOT NULL,
            [CompletedAt] DATETIME2(7) NULL,
            [CreatedBy] NVARCHAR(100) NULL,
            [CreatedAt] DATETIME2(7) NOT NULL,
            [ModifiedBy] NVARCHAR(100) NULL,
            [ModifiedAt] DATETIME2(7) NOT NULL,
            [IsDeleted] BIT NOT NULL CONSTRAINT [DF_WorkflowRuns_IsDeleted] DEFAULT (0),
            CONSTRAINT [CK_WorkflowRuns_Status] CHECK ([Status] IN (N'running', N'succeeded', N'failed', N'canceled')),
            CONSTRAINT [CK_WorkflowRuns_InputJson] CHECK (ISJSON([InputJson]) = 1),
            CONSTRAINT [CK_WorkflowRuns_OutputJson] CHECK (ISJSON([OutputJson]) = 1),
            CONSTRAINT [FK_WorkflowRuns_Workflow] FOREIGN KEY ([WorkflowId]) REFERENCES [dbo].[Workflows] ([Id])
        );

        CREATE INDEX [IX_WorkflowRuns_Workflow_StartedAt]
            ON [dbo].[WorkflowRuns] ([WorkflowId], [StartedAt] DESC)
            INCLUDE ([Status], [IsSimulation]);
    END;

    IF COL_LENGTH(N'[dbo].[WorkflowRuns]', N'EventId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[WorkflowRuns]
            ADD [EventId] NVARCHAR(100) NULL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE [object_id] = OBJECT_ID(N'[dbo].[WorkflowRuns]')
          AND [name] = N'UX_WorkflowRuns_Workflow_Event')
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE UNIQUE INDEX [UX_WorkflowRuns_Workflow_Event]
                ON [dbo].[WorkflowRuns] ([WorkflowId], [EventId])
                WHERE [IsSimulation] = 0 AND [EventId] IS NOT NULL;
        ';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
