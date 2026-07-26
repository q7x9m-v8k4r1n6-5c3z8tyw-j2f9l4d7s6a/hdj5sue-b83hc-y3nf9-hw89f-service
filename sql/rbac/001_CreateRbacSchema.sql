SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[Roles]
	(
		[Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Roles] PRIMARY KEY,
		[Name] NVARCHAR(100) NOT NULL,
		[Code] NVARCHAR(100) NOT NULL,
		[Description] NVARCHAR(500) NULL,
		[IsSystem] BIT NOT NULL CONSTRAINT [DF_Roles_IsSystem] DEFAULT (0),
		[CreatedBy] NVARCHAR(100) NULL,
		[CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Roles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
		[ModifiedBy] NVARCHAR(100) NULL,
		[ModifiedAt] DATETIME2(7) NULL,
		[IsDeleted] BIT NOT NULL CONSTRAINT [DF_Roles_IsDeleted] DEFAULT (0)
	);

	CREATE UNIQUE INDEX [UX_Roles_Code] ON [dbo].[Roles]([Code]) WHERE [IsDeleted] = 0;
	CREATE UNIQUE INDEX [UX_Roles_Name] ON [dbo].[Roles]([Name]) WHERE [IsDeleted] = 0;
END;

IF OBJECT_ID(N'[dbo].[Permissions]', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[Permissions]
	(
		[Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Permissions] PRIMARY KEY,
		[Name] NVARCHAR(150) NOT NULL,
		[Code] NVARCHAR(150) NOT NULL,
		[Description] NVARCHAR(500) NULL,
		[Module] NVARCHAR(100) NOT NULL,
		[Action] NVARCHAR(100) NOT NULL,
		[IsSystem] BIT NOT NULL CONSTRAINT [DF_Permissions_IsSystem] DEFAULT (0),
		[CreatedBy] NVARCHAR(100) NULL,
		[CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_Permissions_CreatedAt] DEFAULT (SYSUTCDATETIME()),
		[ModifiedBy] NVARCHAR(100) NULL,
		[ModifiedAt] DATETIME2(7) NULL,
		[IsDeleted] BIT NOT NULL CONSTRAINT [DF_Permissions_IsDeleted] DEFAULT (0)
	);

	CREATE UNIQUE INDEX [UX_Permissions_Code] ON [dbo].[Permissions]([Code]) WHERE [IsDeleted] = 0;
	CREATE INDEX [IX_Permissions_Module_Action] ON [dbo].[Permissions]([Module], [Action]) WHERE [IsDeleted] = 0;
END;

IF OBJECT_ID(N'[dbo].[UserRoles]', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[UserRoles]
	(
		[Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_UserRoles] PRIMARY KEY,
		[UserId] UNIQUEIDENTIFIER NOT NULL,
		[RoleId] UNIQUEIDENTIFIER NOT NULL,
		[CreatedBy] NVARCHAR(100) NULL,
		[CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_UserRoles_CreatedAt] DEFAULT (SYSUTCDATETIME()),
		[ModifiedBy] NVARCHAR(100) NULL,
		[ModifiedAt] DATETIME2(7) NULL,
		[IsDeleted] BIT NOT NULL CONSTRAINT [DF_UserRoles_IsDeleted] DEFAULT (0),
		CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
		CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id])
	);

	CREATE UNIQUE INDEX [UX_UserRoles_User_Role] ON [dbo].[UserRoles]([UserId], [RoleId]) WHERE [IsDeleted] = 0;
	CREATE INDEX [IX_UserRoles_UserId] ON [dbo].[UserRoles]([UserId]) WHERE [IsDeleted] = 0;
	CREATE INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles]([RoleId]) WHERE [IsDeleted] = 0;
END;

IF OBJECT_ID(N'[dbo].[RolePermissions]', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[RolePermissions]
	(
		[Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_RolePermissions] PRIMARY KEY,
		[RoleId] UNIQUEIDENTIFIER NOT NULL,
		[PermissionId] UNIQUEIDENTIFIER NOT NULL,
		[CreatedBy] NVARCHAR(100) NULL,
		[CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_RolePermissions_CreatedAt] DEFAULT (SYSUTCDATETIME()),
		[ModifiedBy] NVARCHAR(100) NULL,
		[ModifiedAt] DATETIME2(7) NULL,
		[IsDeleted] BIT NOT NULL CONSTRAINT [DF_RolePermissions_IsDeleted] DEFAULT (0),
		CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]),
		CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions]([Id])
	);

	CREATE UNIQUE INDEX [UX_RolePermissions_Role_Permission] ON [dbo].[RolePermissions]([RoleId], [PermissionId]) WHERE [IsDeleted] = 0;
	CREATE INDEX [IX_RolePermissions_RoleId] ON [dbo].[RolePermissions]([RoleId]) WHERE [IsDeleted] = 0;
	CREATE INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions]([PermissionId]) WHERE [IsDeleted] = 0;
END;

COMMIT TRANSACTION;
