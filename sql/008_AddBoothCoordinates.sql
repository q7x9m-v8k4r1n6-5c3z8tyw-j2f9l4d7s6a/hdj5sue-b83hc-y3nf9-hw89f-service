IF COL_LENGTH(N'dbo.Booth', N'MapX') IS NULL
BEGIN
    ALTER TABLE [dbo].[Booth]
        ADD [MapX] FLOAT NULL;
END;

IF COL_LENGTH(N'dbo.Booth', N'MapY') IS NULL
BEGIN
    ALTER TABLE [dbo].[Booth]
        ADD [MapY] FLOAT NULL;
END;
