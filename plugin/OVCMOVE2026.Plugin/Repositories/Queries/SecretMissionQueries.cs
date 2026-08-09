namespace OVCMOVE2026.Plugin.Repositories.Queries;

public static class SecretMissionQueries
{
    private const string Columns = @"
        [Id], [Name], [Description], [IsAssigned], [Location], [TeamId], 
        [ReceivedBy], [ReceivedTime], [SubmittedBy], 
        [QrCodeUrl], 
        [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]";

    public static string GetByIdQuery() => @"
        SELECT * FROM [dbo].[SecretMission]
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string GetOverviewByTeamIdQuery() => @"
        SELECT 
            m.[Id], m.[Name], m.[IsAssigned], 
            CAST(MAX(CASE WHEN e.[FileType] = 'image' THEN 1 ELSE 0 END) AS BIT) AS HasImageEvidence,
            CAST(MAX(CASE WHEN e.[FileType] = 'video' THEN 1 ELSE 0 END) AS BIT) AS HasVideoEvidence
        FROM [dbo].[SecretMission] m
        LEFT JOIN [dbo].[SecretMissionEvidence] e ON m.Id = e.MissionId AND e.IsDeleted = 0
        WHERE m.[TeamId] = @TeamId AND m.[IsDeleted] = 0
        GROUP BY m.[Id], m.[Name], m.[IsAssigned], m.[CreatedAt]
        ORDER BY m.[CreatedAt] DESC;";

    public static string GetDetailByIdAndTeamIdQuery() => @"
        SELECT * FROM [dbo].[SecretMission]
        WHERE [Id] = @Id AND [TeamId] = @TeamId AND [IsDeleted] = 0;";

    public static string GetEvidencesByMissionIdQuery() => @"
        SELECT * FROM [dbo].[SecretMissionEvidence]
        WHERE [MissionId] = @MissionId AND [IsDeleted] = 0;";

    public static string UpdateMissionSubmitStateQuery() => @"
        UPDATE [dbo].[SecretMission]
        SET [SubmittedBy] = @SubmittedBy, [ModifiedBy] = 'system-submit', [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string InsertEvidenceQuery() => @"
        INSERT INTO [dbo].[SecretMissionEvidence] ([Id], [MissionId], [Url], [FileType], [CreatedBy], [CreatedAt])
        VALUES (@Id, @MissionId, @Url, @FileType, @CreatedBy, @CreatedAt);";

    // API Delete gọi 2 câu này
    public static string GetEvidenceByIdQuery() => @"SELECT * FROM [dbo].[SecretMissionEvidence] WHERE [Id] = @Id AND [IsDeleted] = 0;";
    
    public static string DeleteEvidenceQuery() => @"DELETE FROM [dbo].[SecretMissionEvidence] WHERE [Id] = @Id;";
    public static string UpdateClaimQuery() => @"
        UPDATE [dbo].[SecretMission]
        SET [IsAssigned] = 1,
            [TeamId] = @TeamId,
            [ReceivedBy] = @ReceivedBy,
            [ReceivedTime] = @ReceivedTime,
            [ModifiedBy] = 'system-claim-mission',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string GetMissionsWithoutQrCodeQuery() => $@"
        SELECT {Columns}
        FROM [dbo].[SecretMission]
        WHERE [QrCodeUrl] IS NULL AND [IsDeleted] = 0;";

    public static string UpdateQrCodeUrlQuery() => @"
        UPDATE [dbo].[SecretMission]
        SET [QrCodeUrl] = @QrCodeUrl,
            [ModifiedBy] = 'system-generate-qr',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @Id AND [IsDeleted] = 0;";
}