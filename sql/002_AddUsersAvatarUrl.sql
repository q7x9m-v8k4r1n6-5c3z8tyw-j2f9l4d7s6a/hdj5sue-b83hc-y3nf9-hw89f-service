IF COL_LENGTH(N'dbo.Users', N'AvatarUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
        ADD [AvatarUrl] NVARCHAR(2048) NULL;
END;
