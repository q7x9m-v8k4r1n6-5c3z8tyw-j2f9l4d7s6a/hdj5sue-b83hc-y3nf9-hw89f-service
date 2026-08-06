/*
    OVC MOVE - baseline development/test seed

    Run after:
        000_ResetDatabase.sql

    Seeds:
    - Organizer user linked to anhloc280@gmail.com
    - All roles currently defined by UserConstants.RoleCode
    - All permissions currently defined by PermissionCodes
    - Default role-permission assignments
    - Every active role assigned to anhloc280@gmail.com

    The script is idempotent and creates no foreign keys.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @SystemActor NVARCHAR(100) = N'seed';
    DECLARE @LinkedEmail NVARCHAR(320) = N'anhloc280@gmail.com';
    DECLARE @UserId UNIQUEIDENTIFIER;

    /* User */
    SELECT TOP (1) @UserId = [Id]
    FROM [dbo].[Users]
    WHERE [LinkedEmail] = @LinkedEmail
    ORDER BY [IsDeleted], [CreatedAt];

    IF @UserId IS NULL
    BEGIN
        SET @UserId = 'A110C280-0000-4000-8000-000000000001';

        INSERT INTO [dbo].[Users]
        (
            [Id],
            [Username],
            [PasswordHash],
            [LinkedEmail],
            [UserType],
            [DisplayName],
            [ShortName],
            [Status],
            [CreatedBy],
            [CreatedAt],
            [ModifiedBy],
            [ModifiedAt],
            [IsDeleted]
        )
        VALUES
        (
            @UserId,
            NULL,
            NULL,
            @LinkedEmail,
            N'organizer',
            N'Anh Loc',
            N'anhloc280',
            N'active',
            @SystemActor,
            @Now,
            @SystemActor,
            @Now,
            0
        );
    END
    ELSE
    BEGIN
        UPDATE [dbo].[Users]
        SET
            [LinkedEmail] = @LinkedEmail,
            [UserType] = N'organizer',
            [DisplayName] = COALESCE(NULLIF([DisplayName], N''), N'Anh Loc'),
            [ShortName] = COALESCE(NULLIF([ShortName], N''), N'anhloc280'),
            [Status] = N'active',
            [ModifiedBy] = @SystemActor,
            [ModifiedAt] = @Now,
            [IsDeleted] = 0
        WHERE [Id] = @UserId;
    END;

    /* Roles */
    DECLARE @Roles TABLE
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Code] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL
    );

    INSERT INTO @Roles ([Id], [Name], [Code], [Description])
    VALUES
        (
            '86F5647D-C9D7-4F4A-8CD7-1E7AAEE56111',
            N'Administrator',
            N'admin',
            N'Full access across the platform.'
        ),
        (
            'B676A68F-BD4D-4C73-942E-00AB69ED30BB',
            N'Organizer',
            N'organizer',
            N'Operational access for organizers.'
        ),
        (
            '5EB3727E-6417-4FC2-BD87-6D3A1AE6D66E',
            N'Team',
            N'team',
            N'Access for team-facing users.'
        );

    UPDATE existing
    SET
        existing.[Name] = seed.[Name],
        existing.[Description] = seed.[Description],
        existing.[IsSystem] = 1,
        existing.[ModifiedBy] = @SystemActor,
        existing.[ModifiedAt] = @Now,
        existing.[IsDeleted] = 0
    FROM @Roles seed
    CROSS APPLY
    (
        SELECT TOP (1) candidate.[Id]
        FROM [dbo].[Roles] candidate
        WHERE candidate.[Code] = seed.[Code]
        ORDER BY candidate.[IsDeleted], candidate.[CreatedAt] DESC
    ) selected
    INNER JOIN [dbo].[Roles] existing
        ON existing.[Id] = selected.[Id];

    INSERT INTO [dbo].[Roles]
    (
        [Id],
        [Name],
        [Code],
        [Description],
        [IsSystem],
        [CreatedBy],
        [CreatedAt],
        [ModifiedBy],
        [ModifiedAt],
        [IsDeleted]
    )
    SELECT
        seed.[Id],
        seed.[Name],
        seed.[Code],
        seed.[Description],
        1,
        @SystemActor,
        @Now,
        @SystemActor,
        @Now,
        0
    FROM @Roles seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[Roles] existing
        WHERE existing.[Code] = seed.[Code]
    );

    /* Permissions: keep this list synchronized with PermissionCodes.cs. */
    DECLARE @Permissions TABLE
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Code] NVARCHAR(150) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Module] NVARCHAR(100) NOT NULL,
        [Action] NVARCHAR(100) NOT NULL
    );

    INSERT INTO @Permissions
    (
        [Id],
        [Name],
        [Code],
        [Description],
        [Module],
        [Action]
    )
    VALUES
        (
            'B21252C4-D978-44D2-980B-033D7C7A7001',
            N'Auth Profile Read',
            N'auth.profile.read',
            N'View the current user profile and effective access.',
            N'auth',
            N'profile.read'
        ),
        (
            'BF9D7FD7-1B5F-44E7-84E8-D4192B6F1502',
            N'Organizer Read',
            N'organizer.read',
            N'View and search organizers.',
            N'organizer',
            N'read'
        ),
        (
            '604384F8-29EC-4409-968A-5EA1C25E7804',
            N'Organizer Manage Accounts',
            N'organizer.manage_accounts',
            N'Create and manage organizer accounts.',
            N'organizer',
            N'manage_accounts'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740101',
            N'Booth Read',
            N'booth.read',
            N'View booth assignments and operational booth state.',
            N'booth',
            N'read'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740102',
            N'Booth Entry Request',
            N'booth.entry.request',
            N'Request team entry into a booth.',
            N'booth.entry',
            N'request'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740103',
            N'Booth Entry Manage',
            N'booth.entry.manage',
            N'Accept or manage team entry requests at a booth.',
            N'booth.entry',
            N'manage'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740104',
            N'Booth Score Submit',
            N'booth.score.submit',
            N'Submit booth scoring for a team.',
            N'booth.score',
            N'submit'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740105',
            N'Race Read',
            N'race.read',
            N'View assigned or available race information.',
            N'race',
            N'read'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740106',
            N'Race Leaderboard Read',
            N'race.leaderboard.read',
            N'View race leaderboard and scoring logs.',
            N'race.leaderboard',
            N'read'
        ),
        (
            '8359B026-EFB7-4B13-873A-3F189FFB4207',
            N'Race Manage',
            N'race.manage',
            N'Create and update race aggregates.',
            N'race',
            N'manage'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740107',
            N'Race Score Manage',
            N'race.score.manage',
            N'Manually adjust race team scores.',
            N'race.score',
            N'manage'
        ),
        (
            '5E04F9D4-F62A-431B-8A6C-AAA1F0EBE10A',
            N'Team Read',
            N'team.read',
            N'View and search teams.',
            N'team',
            N'read'
        ),
        (
            '03A82242-4FD6-4DBB-8F8D-9F0B00740108',
            N'Team Manage',
            N'team.manage',
            N'Create, update, delete, and reset team accounts.',
            N'team',
            N'manage'
        ),
        (
            'EC2FB60B-A173-4317-B3E6-9AE7D17BA00B',
            N'Image Upload',
            N'image.upload',
            N'Upload images to shared blob storage.',
            N'image',
            N'upload'
        ),
        (
            '55BF899A-0B9E-4B07-BB59-F5A006D8D90C',
            N'RBAC Role Manage',
            N'rbac.role.manage',
            N'Manage RBAC roles.',
            N'rbac.role',
            N'manage'
        ),
        (
            '5A0C7C0C-DB5D-42A1-A1A3-6990A1F8A310',
            N'RBAC Permission Manage',
            N'rbac.permission.manage',
            N'Manage RBAC permissions.',
            N'rbac.permission',
            N'manage'
        ),
        (
            '792AF449-D862-48F9-AD55-CF3B510A6114',
            N'RBAC Assignment Manage',
            N'rbac.assignment.manage',
            N'Manage RBAC assignments.',
            N'rbac.assignment',
            N'manage'
        );

    UPDATE existing
    SET
        existing.[Name] = seed.[Name],
        existing.[Description] = seed.[Description],
        existing.[Module] = seed.[Module],
        existing.[Action] = seed.[Action],
        existing.[IsSystem] = 1,
        existing.[ModifiedBy] = @SystemActor,
        existing.[ModifiedAt] = @Now,
        existing.[IsDeleted] = 0
    FROM @Permissions seed
    CROSS APPLY
    (
        SELECT TOP (1) candidate.[Id]
        FROM [dbo].[Permissions] candidate
        WHERE candidate.[Code] = seed.[Code]
        ORDER BY candidate.[IsDeleted], candidate.[CreatedAt] DESC
    ) selected
    INNER JOIN [dbo].[Permissions] existing
        ON existing.[Id] = selected.[Id];

    INSERT INTO [dbo].[Permissions]
    (
        [Id],
        [Name],
        [Code],
        [Description],
        [Module],
        [Action],
        [IsSystem],
        [CreatedBy],
        [CreatedAt],
        [ModifiedBy],
        [ModifiedAt],
        [IsDeleted]
    )
    SELECT
        seed.[Id],
        seed.[Name],
        seed.[Code],
        seed.[Description],
        seed.[Module],
        seed.[Action],
        1,
        @SystemActor,
        @Now,
        @SystemActor,
        @Now,
        0
    FROM @Permissions seed
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[Permissions] existing
        WHERE existing.[Code] = seed.[Code]
    );

    /*
        Role-permission policy:
        - admin: all permissions
        - organizer: profile, race/booth game operations and team lookup
        - team: profile, assigned race read, leaderboard read and booth entry request
    */
    DECLARE @RolePermissionCodes TABLE
    (
        [RoleCode] NVARCHAR(100) NOT NULL,
        [PermissionCode] NVARCHAR(150) NOT NULL
    );

    INSERT INTO @RolePermissionCodes ([RoleCode], [PermissionCode])
    SELECT N'admin', [Code]
    FROM @Permissions;

    INSERT INTO @RolePermissionCodes ([RoleCode], [PermissionCode])
    VALUES
        (N'organizer', N'auth.profile.read'),
        (N'organizer', N'booth.read'),
        (N'organizer', N'booth.entry.manage'),
        (N'organizer', N'booth.score.submit'),
        (N'organizer', N'race.read'),
        (N'organizer', N'race.leaderboard.read'),
        (N'organizer', N'team.read'),
        (N'team', N'auth.profile.read'),
        (N'team', N'booth.entry.request'),
        (N'team', N'race.read'),
        (N'team', N'race.leaderboard.read'),
        (N'team', N'team.read');

    INSERT INTO [dbo].[RolePermissions]
    (
        [Id],
        [RoleId],
        [PermissionId],
        [CreatedBy],
        [CreatedAt],
        [ModifiedBy],
        [ModifiedAt],
        [IsDeleted]
    )
    SELECT
        NEWID(),
        r.[Id],
        p.[Id],
        @SystemActor,
        @Now,
        @SystemActor,
        @Now,
        0
    FROM @RolePermissionCodes m
    INNER JOIN [dbo].[Roles] r
        ON r.[Code] = m.[RoleCode]
       AND r.[IsDeleted] = 0
    INNER JOIN [dbo].[Permissions] p
        ON p.[Code] = m.[PermissionCode]
       AND p.[IsDeleted] = 0
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [dbo].[RolePermissions] existing
        WHERE existing.[RoleId] = r.[Id]
          AND existing.[PermissionId] = p.[Id]
          AND existing.[IsDeleted] = 0
    );

    /* Grant every active role to the seeded user. */
    INSERT INTO [dbo].[UserRoles]
    (
        [Id],
        [UserId],
        [RoleId],
        [CreatedBy],
        [CreatedAt],
        [ModifiedBy],
        [ModifiedAt],
        [IsDeleted]
    )
    SELECT
        NEWID(),
        @UserId,
        r.[Id],
        @SystemActor,
        @Now,
        @SystemActor,
        @Now,
        0
    FROM [dbo].[Roles] r
    WHERE r.[IsDeleted] = 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM [dbo].[UserRoles] existing
          WHERE existing.[UserId] = @UserId
            AND existing.[RoleId] = r.[Id]
            AND existing.[IsDeleted] = 0
      );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
