/*
    OVC MOVE - destructive database reset

    WARNING:
    - Run this script against the intended development/test database only.
    - Every user table in the current database is dropped.
    - The recreated schema intentionally contains NO FOREIGN KEY constraints.
    - Column names also account for the current Dapper queries where the
      persistence shape differs slightly from the domain projection.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Sql NVARCHAR(MAX) = N'';

    -- Temporal tables cannot be dropped while system versioning is enabled.
    SELECT @Sql = @Sql
        + N'ALTER TABLE '
        + QUOTENAME(OBJECT_SCHEMA_NAME([object_id]))
        + N'.'
        + QUOTENAME(OBJECT_NAME([object_id]))
        + N' SET (SYSTEM_VERSIONING = OFF);'
        + CHAR(10)
    FROM sys.tables
    WHERE [temporal_type] = 2;

    IF LEN(@Sql) > 0
    BEGIN
        EXEC sys.sp_executesql @Sql;
    END;

    -- Remove legacy foreign keys so all existing user tables can be dropped
    -- regardless of their dependency order.
    SET @Sql = N'';

    SELECT @Sql = @Sql
        + N'ALTER TABLE '
        + QUOTENAME(OBJECT_SCHEMA_NAME([parent_object_id]))
        + N'.'
        + QUOTENAME(OBJECT_NAME([parent_object_id]))
        + N' DROP CONSTRAINT '
        + QUOTENAME([name])
        + N';'
        + CHAR(10)
    FROM sys.foreign_keys;

    IF LEN(@Sql) > 0
    BEGIN
        EXEC sys.sp_executesql @Sql;
    END;

    -- Drop every user table in the selected database.
    SET @Sql = N'';

    SELECT @Sql = @Sql
        + N'DROP TABLE '
        + QUOTENAME(SCHEMA_NAME([schema_id]))
        + N'.'
        + QUOTENAME([name])
        + N';'
        + CHAR(10)
    FROM sys.tables;

    IF LEN(@Sql) > 0
    BEGIN
        EXEC sys.sp_executesql @Sql;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;

/*
    Start schema creation after the destructive reset.
    Indexes that refer to the new Users shape are executed dynamically so
    SQL Server cannot compile them against the old Users table.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* Users */
    CREATE TABLE [dbo].[Users]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_Users] PRIMARY KEY
            CONSTRAINT [DF_Users_Id] DEFAULT (NEWID()),
        [Username] NVARCHAR(255) NULL,
        [PasswordHash] NVARCHAR(500) NULL,
        [LinkedEmail] NVARCHAR(320) NOT NULL,
        [UserType] NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_Users_UserType] DEFAULT (N'team'),
        [DisplayName] NVARCHAR(255) NULL,
        [ShortName] NVARCHAR(100) NULL,
        [Status] NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_Users_Status] DEFAULT (N'active'),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Users_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Users_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_Users_IsDeleted] DEFAULT (0),
        CONSTRAINT [CK_Users_UserType]
            CHECK ([UserType] IN (N'team', N'organizer'))
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_Users_LinkedEmail]
            ON [dbo].[Users] ([LinkedEmail])
            WHERE [IsDeleted] = 0;

        CREATE UNIQUE INDEX [UX_Users_Username]
            ON [dbo].[Users] ([Username])
            WHERE [Username] IS NOT NULL AND [IsDeleted] = 0;

        CREATE UNIQUE INDEX [UX_Users_ShortName]
            ON [dbo].[Users] ([ShortName])
            WHERE [ShortName] IS NOT NULL AND [IsDeleted] = 0;

        CREATE INDEX [IX_Users_UserType_Status]
            ON [dbo].[Users] ([UserType], [Status])
            WHERE [IsDeleted] = 0;
    ';

    /* Race aggregate */
    CREATE TABLE [dbo].[Race]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_Race] PRIMARY KEY
            CONSTRAINT [DF_Race_Id] DEFAULT (NEWID()),
        [RaceName] NVARCHAR(255) NOT NULL,
        [TimeStart] DATETIME2(7) NOT NULL,
        [TimeEnd] DATETIME2(7) NOT NULL,
        [Place] NVARCHAR(255) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL
            CONSTRAINT [DF_Race_Status] DEFAULT (N'draft'),
        [CoverUrl] NVARCHAR(2048) NULL,
        [IsToggledLeaderboard] BIT NOT NULL
            CONSTRAINT [DF_Race_IsToggledLeaderboard] DEFAULT (0),
        [IsHiddenPoint] BIT NOT NULL
            CONSTRAINT [DF_Race_IsHiddenPoint] DEFAULT (0),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Race_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Race_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_Race_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE INDEX [IX_Race_Status_CreatedAt]
            ON [dbo].[Race] ([Status], [CreatedAt])
            WHERE [IsDeleted] = 0;
    ';

    CREATE TABLE [dbo].[Booth]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_Booth] PRIMARY KEY
            CONSTRAINT [DF_Booth_Id] DEFAULT (NEWID()),
        [Name] NVARCHAR(255) NOT NULL,
        [Place] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL
            CONSTRAINT [DF_Booth_Description] DEFAULT (N''),
        [RaceID] UNIQUEIDENTIFIER NOT NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Booth_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Booth_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_Booth_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE INDEX [IX_Booth_RaceID]
            ON [dbo].[Booth] ([RaceID]);
    ';

    CREATE TABLE [dbo].[BoothOrganizer]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_BoothOrganizer] PRIMARY KEY
            CONSTRAINT [DF_BoothOrganizer_Id] DEFAULT (NEWID()),
        [BoothId] UNIQUEIDENTIFIER NOT NULL,
        [OrganizerId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_BoothOrganizer_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_BoothOrganizer_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_BoothOrganizer_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_BoothOrganizer_BoothId_OrganizerId]
            ON [dbo].[BoothOrganizer] ([BoothId], [OrganizerId])
            WHERE [IsDeleted] = 0;

        CREATE INDEX [IX_BoothOrganizer_OrganizerId]
            ON [dbo].[BoothOrganizer] ([OrganizerId])
            WHERE [IsDeleted] = 0;
    ';

    CREATE TABLE [dbo].[RaceTeam]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_RaceTeam] PRIMARY KEY
            CONSTRAINT [DF_RaceTeam_Id] DEFAULT (NEWID()),
        [RaceID] UNIQUEIDENTIFIER NOT NULL,
        [TeamID] UNIQUEIDENTIFIER NOT NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RaceTeam_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RaceTeam_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_RaceTeam_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_RaceTeam_RaceID_TeamID]
            ON [dbo].[RaceTeam] ([RaceID], [TeamID])
            WHERE [IsDeleted] = 0;
    ';

    CREATE TABLE [dbo].[RaceOrganizer]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_RaceOrganizer] PRIMARY KEY
            CONSTRAINT [DF_RaceOrganizer_Id] DEFAULT (NEWID()),
        [RaceID] UNIQUEIDENTIFIER NOT NULL,
        [OrganizerID] UNIQUEIDENTIFIER NOT NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RaceOrganizer_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RaceOrganizer_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_RaceOrganizer_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_RaceOrganizer_RaceID_OrganizerID]
            ON [dbo].[RaceOrganizer] ([RaceID], [OrganizerID])
            WHERE [IsDeleted] = 0;
    ';

    /* Authentication */
    CREATE TABLE [dbo].[RefreshTokens]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_RefreshTokens] PRIMARY KEY
            CONSTRAINT [DF_RefreshTokens_Id] DEFAULT (NEWID()),
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [SessionId] UNIQUEIDENTIFIER NOT NULL,
        [FamilyId] UNIQUEIDENTIFIER NOT NULL,
        -- Retained only for compatibility with older deployments; new code never stores a raw token.
        [Token] NVARCHAR(500) NULL,
        [TokenHash] NVARCHAR(500) NOT NULL,
        [ReplacedByTokenId] UNIQUEIDENTIFIER NULL,
        [RevokedAt] DATETIME2(7) NULL,
        [ExpiryDate] DATETIME2(7) NOT NULL,
        [IsRevoked] BIT NOT NULL
            CONSTRAINT [DF_RefreshTokens_IsRevoked] DEFAULT (0),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RefreshTokens_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RefreshTokens_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_RefreshTokens_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_RefreshTokens_TokenHash]
            ON [dbo].[RefreshTokens] ([TokenHash]);

        CREATE INDEX [IX_RefreshTokens_UserId]
            ON [dbo].[RefreshTokens] ([UserId]);

        CREATE INDEX [IX_RefreshTokens_FamilyId_IsRevoked]
            ON [dbo].[RefreshTokens] ([FamilyId], [IsRevoked]);

        CREATE INDEX [IX_RefreshTokens_ExpiryDate]
            ON [dbo].[RefreshTokens] ([ExpiryDate]);
    ';

    /* RBAC */
    CREATE TABLE [dbo].[Roles]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_Roles] PRIMARY KEY
            CONSTRAINT [DF_Roles_Id] DEFAULT (NEWID()),
        [Name] NVARCHAR(100) NOT NULL,
        [Code] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsSystem] BIT NOT NULL
            CONSTRAINT [DF_Roles_IsSystem] DEFAULT (0),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Roles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Roles_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_Roles_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_Roles_Code]
            ON [dbo].[Roles] ([Code])
            WHERE [IsDeleted] = 0;

        CREATE UNIQUE INDEX [UX_Roles_Name]
            ON [dbo].[Roles] ([Name])
            WHERE [IsDeleted] = 0;
    ';

    CREATE TABLE [dbo].[Permissions]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_Permissions] PRIMARY KEY
            CONSTRAINT [DF_Permissions_Id] DEFAULT (NEWID()),
        [Name] NVARCHAR(150) NOT NULL,
        [Code] NVARCHAR(150) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Module] NVARCHAR(100) NOT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [IsSystem] BIT NOT NULL
            CONSTRAINT [DF_Permissions_IsSystem] DEFAULT (0),
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Permissions_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_Permissions_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_Permissions_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_Permissions_Code]
            ON [dbo].[Permissions] ([Code])
            WHERE [IsDeleted] = 0;

        CREATE INDEX [IX_Permissions_Module_Action]
            ON [dbo].[Permissions] ([Module], [Action])
            WHERE [IsDeleted] = 0;
    ';

    CREATE TABLE [dbo].[UserRoles]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_UserRoles] PRIMARY KEY
            CONSTRAINT [DF_UserRoles_Id] DEFAULT (NEWID()),
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_UserRoles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_UserRoles_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_UserRoles_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_UserRoles_UserId_RoleId]
            ON [dbo].[UserRoles] ([UserId], [RoleId])
            WHERE [IsDeleted] = 0;

        CREATE INDEX [IX_UserRoles_RoleId]
            ON [dbo].[UserRoles] ([RoleId])
            WHERE [IsDeleted] = 0;
    ';

    CREATE TABLE [dbo].[RolePermissions]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT [PK_RolePermissions] PRIMARY KEY
            CONSTRAINT [DF_RolePermissions_Id] DEFAULT (NEWID()),
        [RoleId] UNIQUEIDENTIFIER NOT NULL,
        [PermissionId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RolePermissions_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2(7) NOT NULL
            CONSTRAINT [DF_RolePermissions_ModifiedAt] DEFAULT (SYSUTCDATETIME()),
        [IsDeleted] BIT NOT NULL
            CONSTRAINT [DF_RolePermissions_IsDeleted] DEFAULT (0)
    );

    EXEC sys.sp_executesql N'
        CREATE UNIQUE INDEX [UX_RolePermissions_RoleId_PermissionId]
            ON [dbo].[RolePermissions] ([RoleId], [PermissionId])
            WHERE [IsDeleted] = 0;

        CREATE INDEX [IX_RolePermissions_PermissionId]
            ON [dbo].[RolePermissions] ([PermissionId])
            WHERE [IsDeleted] = 0;
    ';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
