namespace OVCMOVE.Infrastructure.Persistence.Queries;

public static class WorkflowQueries
{
    public const string SelectByRace = """
        SELECT [Id], [CardId], [RaceId], [CardKey], [CardName], [Name], [Description],
               [TriggerType], [Status], [Version], [DefinitionJson],
               [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Workflows]
        WHERE [RaceId] = @RaceId
          AND [IsDeleted] = 0
          AND (@CardKey IS NULL OR [CardKey] = @CardKey)
        ORDER BY [CardName], [ModifiedAt] DESC;
        """;

    public const string SelectById = """
        SELECT [Id], [CardId], [RaceId], [CardKey], [CardName], [Name], [Description],
               [TriggerType], [Status], [Version], [DefinitionJson],
               [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[Workflows]
        WHERE [Id] = @WorkflowId AND [IsDeleted] = 0;
        """;

    public const string Insert = """
        INSERT INTO [dbo].[Workflows]
        ([Id], [CardId], [RaceId], [CardKey], [CardName], [Name], [Description],
         [TriggerType], [Status], [Version], [DefinitionJson], [CreatedBy],
         [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted])
        VALUES
        (@Id, @CardId, @RaceId, @CardKey, @CardName, @Name, @Description,
         @TriggerType, @Status, @Version, @DefinitionJson, @CreatedBy,
         @CreatedAt, @ModifiedBy, @ModifiedAt, @IsDeleted);
        """;

    public const string Update = """
        UPDATE [dbo].[Workflows]
        SET [CardId] = @CardId,
            [CardKey] = @CardKey,
            [CardName] = @CardName,
            [Name] = @Name,
            [Description] = @Description,
            [TriggerType] = @TriggerType,
            [Status] = @Status,
            [Version] = @Version,
            [DefinitionJson] = @DefinitionJson,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id
          AND [ModifiedAt] = @ExpectedModifiedAt
          AND [IsDeleted] = 0;
        """;

    public const string SoftDelete = """
        UPDATE [dbo].[Workflows]
        SET [IsDeleted] = 1, [ModifiedBy] = @Actor, [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @WorkflowId AND [IsDeleted] = 0;
        """;

    public const string InsertRun = """
        INSERT INTO [dbo].[WorkflowRuns]
        ([Id], [WorkflowId], [RaceId], [CardKey], [TriggerType], [EventId], [Status],
         [IsSimulation], [InputJson], [OutputJson], [Error], [StartedAt],
         [CompletedAt], [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt],
         [IsDeleted])
        VALUES
        (@Id, @WorkflowId, @RaceId, @CardKey, @TriggerType, @EventId, @Status,
         @IsSimulation, @InputJson, @OutputJson, @Error, @StartedAt,
         @CompletedAt, @CreatedBy, @CreatedAt, @ModifiedBy, @ModifiedAt,
         @IsDeleted);
        """;

    public const string CompleteRun = """
        UPDATE [dbo].[WorkflowRuns]
        SET [Status] = @Status,
            [OutputJson] = @OutputJson,
            [Error] = @Error,
            [CompletedAt] = @CompletedAt,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedAt] = @ModifiedAt
        WHERE [Id] = @Id AND [Status] = N'running' AND [IsDeleted] = 0;
        """;

    public const string SelectRuns = """
        SELECT TOP (@Limit) [Id], [WorkflowId], [RaceId], [CardKey],
               [TriggerType], [EventId], [Status], [IsSimulation], [InputJson],
               [OutputJson], [Error], [StartedAt], [CompletedAt], [CreatedBy],
               [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]
        FROM [dbo].[WorkflowRuns]
        WHERE [WorkflowId] = @WorkflowId AND [IsDeleted] = 0
        ORDER BY [StartedAt] DESC;
        """;
}
