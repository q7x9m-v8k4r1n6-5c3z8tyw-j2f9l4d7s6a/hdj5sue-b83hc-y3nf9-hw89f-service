namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class FunctionCardQueries
{
    private const string ReadColumns = """
        FC.[Id], FC.[RaceId], FC.[TeamId],
        COALESCE(NULLIF(U.[DisplayName], N''), U.[Username], U.[LinkedEmail]) AS [TeamName],
        FC.[CardKey], FC.[Name], FC.[Description], FC.[Category],
        FC.[BackgroundUrl], FC.[InputsJson], W.[Id] AS [WorkflowId],
        W.[Name] AS [WorkflowName], W.[Status] AS [WorkflowStatus],
        FC.[CreatedAt], FC.[ModifiedAt]
        """;

    public static readonly string SelectByRace = $"""
        SELECT {ReadColumns}
        FROM [dbo].[FunctionCards] FC
        LEFT JOIN [dbo].[Users] U ON U.[Id] = FC.[TeamId] AND U.[IsDeleted] = 0
        LEFT JOIN [dbo].[Workflows] W ON W.[CardId] = FC.[Id] AND W.[IsDeleted] = 0
        WHERE FC.[RaceId] = @RaceId AND FC.[IsDeleted] = 0
        ORDER BY FC.[CreatedAt] DESC;
        """;

    public static readonly string SelectDetail = $"""
        SELECT {ReadColumns}
        FROM [dbo].[FunctionCards] FC
        LEFT JOIN [dbo].[Users] U ON U.[Id] = FC.[TeamId] AND U.[IsDeleted] = 0
        LEFT JOIN [dbo].[Workflows] W ON W.[CardId] = FC.[Id] AND W.[IsDeleted] = 0
        WHERE FC.[Id] = @CardId AND FC.[IsDeleted] = 0;
        """;

    public const string SelectEntityById = """
        SELECT [Id], [RaceId], [TeamId], [CardKey], [Name], [Description],
               [Category], [BackgroundUrl], [InputsJson], [CreatedBy], [CreatedAt],
               [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[FunctionCards]
        WHERE [Id] = @CardId AND [IsDeleted] = 0;
        """;

    public const string SelectEntityByKey = """
        SELECT [Id], [RaceId], [TeamId], [CardKey], [Name], [Description],
               [Category], [BackgroundUrl], [InputsJson], [CreatedBy], [CreatedAt],
               [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[FunctionCards]
        WHERE [RaceId] = @RaceId AND [CardKey] = @CardKey AND [IsDeleted] = 0;
        """;

    public const string Insert = """
        INSERT INTO [dbo].[FunctionCards]
        ([Id], [RaceId], [TeamId], [CardKey], [Name], [Description], [Category],
         [BackgroundUrl], [InputsJson], [CreatedBy], [CreatedAt], [ModifiedBy],
         [ModifiedAt], [IsDeleted])
        VALUES
        (@Id, @RaceId, @TeamId, @CardKey, @Name, @Description, @Category,
         @BackgroundUrl, @InputsJson, @CreatedBy, @CreatedAt, @ModifiedBy,
         @ModifiedAt, @IsDeleted);
        """;

    public const string Update = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;
        UPDATE [dbo].[FunctionCards]
        SET [CardKey] = @CardKey,
            [Name] = @Name,
            [Description] = @Description,
            [Category] = @Category,
            [BackgroundUrl] = @BackgroundUrl,
            [InputsJson] = @InputsJson,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id AND [ModifiedAt] = @ExpectedModifiedAt AND [IsDeleted] = 0;

        DECLARE @Updated INT = @@ROWCOUNT;
        IF @Updated = 1
        BEGIN
            UPDATE [dbo].[Workflows]
            SET [CardKey] = @CardKey,
                [CardName] = @Name
            WHERE [CardId] = @Id AND [IsDeleted] = 0;
        END;
        COMMIT TRANSACTION;
        SELECT @Updated;
        """;

    public const string AssignTeam = """
        UPDATE [dbo].[FunctionCards]
        SET [TeamId] = @TeamId, [ModifiedBy] = @Actor, [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @CardId
          AND [ModifiedAt] = @ExpectedModifiedAt
          AND [IsDeleted] = 0;
        """;

    public const string SoftDelete = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;
        UPDATE [dbo].[FunctionCards]
        SET [IsDeleted] = 1, [TeamId] = NULL,
            [ModifiedBy] = @Actor, [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @CardId AND [IsDeleted] = 0;

        DECLARE @Deleted INT = @@ROWCOUNT;
        IF @Deleted = 1
        BEGIN
            UPDATE [dbo].[Workflows]
            SET [IsDeleted] = 1, [ModifiedBy] = @Actor, [ModifiedAt] = @ModifiedAt
            WHERE [CardId] = @CardId AND [IsDeleted] = 0;
        END;
        COMMIT TRANSACTION;
        SELECT @Deleted;
        """;

    public const string SelectByTeamId = """
        SELECT 
            FC.[Id] AS [CardId],
            FC.[BackgroundUrl] AS [CardUrl],
            FC.[Name] AS [CardName],
            W.[TriggerType] AS [CardType],
            W.[Status] AS [CardStatus]
        FROM [dbo].[FunctionCards] FC
        INNER JOIN [dbo].[Workflows] W ON W.[CardId] = FC.[Id] AND W.[IsDeleted] = 0
        WHERE FC.[RaceId] = @RaceId 
          AND FC.[TeamId] = @TeamId 
          AND FC.[IsDeleted] = 0
          AND W.[Status] <> @ExcludedStatus
        ORDER BY FC.[CreatedAt] DESC;
        """;

    public const string SelectCardDescriptionById = """
        SELECT 
            FC.[Description]
        FROM [dbo].[FunctionCards] FC
        INNER JOIN [dbo].[Workflows] W ON W.[CardId] = FC.[Id] AND W.[IsDeleted] = 0
        WHERE FC.[Id] = @CardId 
          AND FC.[TeamId] = @TeamId 
          AND FC.[IsDeleted] = 0
          AND W.[Status] <> @ExcludedStatus;
        """;
}
