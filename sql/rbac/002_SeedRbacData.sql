SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @SystemActor NVARCHAR(100) = N'system';

DECLARE @AdminRoleId UNIQUEIDENTIFIER = '86F5647D-C9D7-4F4A-8CD7-1E7AAEE56111';
DECLARE @OrganizerRoleId UNIQUEIDENTIFIER = 'B676A68F-BD4D-4C73-942E-00AB69ED30BB';
DECLARE @TeamRoleId UNIQUEIDENTIFIER = '5EB3727E-6417-4FC2-BD87-6D3A1AE6D66E';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Code] = N'admin' AND [IsDeleted] = 0)
BEGIN
	INSERT INTO [dbo].[Roles] ([Id], [Name], [Code], [Description], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
	VALUES (@AdminRoleId, N'Administrator', N'admin', N'Full access across the platform.', 1, @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Code] = N'organizer' AND [IsDeleted] = 0)
BEGIN
	INSERT INTO [dbo].[Roles] ([Id], [Name], [Code], [Description], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
	VALUES (@OrganizerRoleId, N'Organizer', N'organizer', N'Operational access for organizers.', 1, @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Code] = N'team' AND [IsDeleted] = 0)
BEGIN
	INSERT INTO [dbo].[Roles] ([Id], [Name], [Code], [Description], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
	VALUES (@TeamRoleId, N'Team', N'team', N'Access for team-facing users.', 1, @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0);
END;

DECLARE @Permissions TABLE
(
	[Id] UNIQUEIDENTIFIER NOT NULL,
	[Name] NVARCHAR(150) NOT NULL,
	[Code] NVARCHAR(150) NOT NULL,
	[Description] NVARCHAR(500) NULL,
	[Module] NVARCHAR(100) NOT NULL,
	[Action] NVARCHAR(100) NOT NULL
);

INSERT INTO @Permissions ([Id], [Name], [Code], [Description], [Module], [Action])
VALUES
('B21252C4-D978-44D2-980B-033D7C7A7001', N'Auth Profile Read', N'auth.profile.read', N'View current user profile and effective access.', N'auth', N'profile.read'),
('BF9D7FD7-1B5F-44E7-84E8-D4192B6F1502', N'Organizer Read', N'organizer.read', N'View and search organizers.', N'organizer', N'read'),
('604384F8-29EC-4409-968A-5EA1C25E7804', N'Organizer Manage Accounts', N'organizer.manage_accounts', N'Create and manage organizer accounts.', N'organizer', N'manage_accounts'),
('8359B026-EFB7-4B13-873A-3F189FFB4207', N'Race Manage', N'race.manage', N'Create and update race aggregates.', N'race', N'manage'),
('D6054FC1-8EC7-4239-B7CD-4F1DFE4AF109', N'Image Upload', N'image.upload', N'Upload images.', N'image', N'upload'),
('5E04F9D4-F62A-431B-8A6C-AAA1F0EBE10A', N'Team Read', N'team.read', N'View and search teams.', N'team', N'read'),
('55BF899A-0B9E-4B07-BB59-F5A006D8D90C', N'RBAC Role Manage', N'rbac.role.manage', N'Manage RBAC roles.', N'rbac.role', N'manage'),
('5A0C7C0C-DB5D-42A1-A1A3-6990A1F8A310', N'RBAC Permission Manage', N'rbac.permission.manage', N'Manage RBAC permissions.', N'rbac.permission', N'manage'),
('792AF449-D862-48F9-AD55-CF3B510A6114', N'RBAC Assignment Manage', N'rbac.assignment.manage', N'Manage RBAC assignments.', N'rbac.assignment', N'manage');

INSERT INTO [dbo].[Permissions] ([Id], [Name], [Code], [Description], [Module], [Action], [IsSystem], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
SELECT p.[Id], p.[Name], p.[Code], p.[Description], p.[Module], p.[Action], 1, @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0
FROM @Permissions p
WHERE NOT EXISTS (
	SELECT 1
	FROM [dbo].[Permissions] existing
	WHERE existing.[Code] = p.[Code]
	  AND existing.[IsDeleted] = 0);

;WITH AdminPermissions AS
(
	SELECT [Id] AS [PermissionId]
	FROM [dbo].[Permissions]
	WHERE [IsDeleted] = 0
),
OrganizerPermissions AS
(
	SELECT [Id] AS [PermissionId]
	FROM [dbo].[Permissions]
	WHERE [Code] IN
	(
		N'auth.profile.read',
		N'organizer.read',
		N'race.manage',
		N'image.upload',
		N'team.read'
	)
	  AND [IsDeleted] = 0
),
TeamPermissions AS
(
	SELECT [Id] AS [PermissionId]
	FROM [dbo].[Permissions]
	WHERE [Code] IN
	(
		N'auth.profile.read',
		N'team.read'
	)
	  AND [IsDeleted] = 0
)
INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
SELECT NEWID(), @AdminRoleId, ap.[PermissionId], @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0
FROM AdminPermissions ap
WHERE NOT EXISTS (
	SELECT 1
	FROM [dbo].[RolePermissions] rp
	WHERE rp.[RoleId] = @AdminRoleId
	  AND rp.[PermissionId] = ap.[PermissionId]
	  AND rp.[IsDeleted] = 0);

INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
SELECT NEWID(), @OrganizerRoleId, op.[PermissionId], @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0
FROM OrganizerPermissions op
WHERE NOT EXISTS (
	SELECT 1
	FROM [dbo].[RolePermissions] rp
	WHERE rp.[RoleId] = @OrganizerRoleId
	  AND rp.[PermissionId] = op.[PermissionId]
	  AND rp.[IsDeleted] = 0);

INSERT INTO [dbo].[RolePermissions] ([Id], [RoleId], [PermissionId], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
SELECT NEWID(), @TeamRoleId, tp.[PermissionId], @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0
FROM TeamPermissions tp
WHERE NOT EXISTS (
	SELECT 1
	FROM [dbo].[RolePermissions] rp
	WHERE rp.[RoleId] = @TeamRoleId
	  AND rp.[PermissionId] = tp.[PermissionId]
	  AND rp.[IsDeleted] = 0);

INSERT INTO [dbo].[UserRoles] ([Id], [UserId], [RoleId], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
SELECT NEWID(), u.[Id], r.[Id], @SystemActor, SYSUTCDATETIME(), @SystemActor, SYSUTCDATETIME(), 0
FROM [dbo].[Users] u
INNER JOIN [dbo].[Roles] r ON r.[Code] = LOWER(LTRIM(RTRIM(ISNULL(u.[Role], N'team')))) AND r.[IsDeleted] = 0
WHERE NOT EXISTS (
	SELECT 1
	FROM [dbo].[UserRoles] ur
	WHERE ur.[UserId] = u.[Id]
	  AND ur.[RoleId] = r.[Id]
	  AND ur.[IsDeleted] = 0);

COMMIT TRANSACTION;
