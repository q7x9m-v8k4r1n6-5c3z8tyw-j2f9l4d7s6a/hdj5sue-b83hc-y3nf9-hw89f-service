IF COL_LENGTH(N'dbo.Race', N'IsShowHiddenBooths') IS NULL
BEGIN
    ALTER TABLE [dbo].[Race]
        ADD [IsShowHiddenBooths] BIT NOT NULL CONSTRAINT DF_Race_IsShowHiddenBooths DEFAULT 0,
            [IsHideBoothDescription] BIT NOT NULL CONSTRAINT DF_Race_IsHideBoothDescription DEFAULT 0,
            [IsDisabledBoothStatus] BIT NOT NULL CONSTRAINT DF_Race_IsDisabledBoothStatus DEFAULT 0;
END;
