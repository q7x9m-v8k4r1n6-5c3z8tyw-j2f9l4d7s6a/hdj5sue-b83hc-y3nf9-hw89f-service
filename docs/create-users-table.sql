IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_Users] PRIMARY KEY
            CONSTRAINT [DF_Users_Id] DEFAULT NEWID(),
        [Username] NVARCHAR(255) NULL,
        [PasswordHash] NVARCHAR(255) NULL,
        [Email] NVARCHAR(255) NOT NULL,
        [Role] NVARCHAR(50) NOT NULL,
        [DisplayName] NVARCHAR(255) NULL,
        [Status] NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_Users_Status] DEFAULT N'Active',
        [CreatedBy] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME2 NOT NULL
            CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] NVARCHAR(255) NULL,
        [ModifiedAt] DATETIME2 NULL,
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_Users_IsDeleted] DEFAULT 0
    );

    CREATE UNIQUE INDEX [UX_Users_Username]
        ON [dbo].[Users] ([Username])
        WHERE [Username] IS NOT NULL;

    CREATE UNIQUE INDEX [UX_Users_Email]
        ON [dbo].[Users] ([Email]);

    CREATE INDEX [IX_Users_Status]
        ON [dbo].[Users] ([Status]);
END

IF NOT EXISTS (
    SELECT 1
    FROM [dbo].[Users]
    WHERE [Username] = N'admin'
)
BEGIN
    INSERT INTO [dbo].[Users]
        ([Username], [PasswordHash], [Email], [Role], [DisplayName], [Status])
    VALUES
        (N'admin', N'admin123', N'admin@oispvolunteerclub.com', N'Admin', N'Local Admin', N'Active');
END
