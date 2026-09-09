IF COL_LENGTH(N'dbo.Race', N'MapImageUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[Race]
        ADD [MapImageUrl] NVARCHAR(2048) NULL;
END;
