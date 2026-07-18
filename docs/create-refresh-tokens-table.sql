IF OBJECT_ID(N'[dbo].[RefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RefreshTokens]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_RefreshTokens] PRIMARY KEY
            CONSTRAINT [DF_RefreshTokens_Id] DEFAULT NEWID(),
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [Token] NVARCHAR(512) NOT NULL,
        [ExpiryDate] DATETIME2 NOT NULL,
        [IsRevoked] BIT NOT NULL
            CONSTRAINT [DF_RefreshTokens_IsRevoked] DEFAULT 0,
        [CreatedBy] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME2 NOT NULL
            CONSTRAINT [DF_RefreshTokens_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] NVARCHAR(255) NULL,
        [ModifiedAt] DATETIME2 NULL,
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_RefreshTokens_IsDeleted] DEFAULT 0
    );

    CREATE UNIQUE INDEX [UX_RefreshTokens_Token]
        ON [dbo].[RefreshTokens] ([Token]);

    CREATE INDEX [IX_RefreshTokens_UserId]a
        ON [dbo].[RefreshTokens] ([UserId]);

    CREATE INDEX [IX_RefreshTokens_ExpiryDate]
        ON [dbo].[RefreshTokens] ([ExpiryDate]);
END
