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

    IF OBJECT_ID(N'dbo.RaceMessage', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[RaceMessage]
        (
            [Id] UNIQUEIDENTIFIER NOT NULL,
            [RaceId] UNIQUEIDENTIFIER NOT NULL,
            [SenderId] UNIQUEIDENTIFIER NULL,
            [SenderName] NVARCHAR(255) NOT NULL,
            [RecipientKeysJson] NVARCHAR(MAX) NOT NULL,
            [RecipientLabelsJson] NVARCHAR(MAX) NOT NULL,
            [Body] NVARCHAR(MAX) NOT NULL,
            [CreatedBy] NVARCHAR(255) NULL,
            [CreatedAt] DATETIME2 NOT NULL,
            [ModifiedBy] NVARCHAR(255) NULL,
            [ModifiedAt] DATETIME2 NOT NULL,
            [IsDeleted] BIT NOT NULL CONSTRAINT [DF_RaceMessage_IsDeleted] DEFAULT (0),
            CONSTRAINT [PK_RaceMessage] PRIMARY KEY CLUSTERED ([Id]),
            CONSTRAINT [FK_RaceMessage_Race] FOREIGN KEY ([RaceId])
                REFERENCES [dbo].[Race] ([Id])
        );

        CREATE INDEX [IX_RaceMessage_RaceId_CreatedAt]
            ON [dbo].[RaceMessage] ([RaceId], [CreatedAt] DESC)
            INCLUDE ([SenderName], [Body], [IsDeleted]);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
