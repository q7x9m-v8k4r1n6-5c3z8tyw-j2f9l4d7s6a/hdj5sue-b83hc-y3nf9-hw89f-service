namespace OVCMOVE2026.Plugin.Repositories.Queries;

public static class SecretMissionQueries
{
    private const string Columns = @"
        [Id], [Name], [Description], [IsAssigned], [Location], [TeamId], 
        [ReceivedBy], [ReceivedTime], [SubmittedBy], [SubmittedTime], 
        [QrCodeUrl], [EvidenceVideoUrl], [EvidenceImageUrl], 
        [CreatedBy], [CreatedAt], [ModifiedBy], [ModifiedAt], [IsDeleted]";

    public static string GetByIdQuery() => $@"
        SELECT {Columns}
        FROM [dbo].[SecretMission]
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string UpdateEvidenceQuery() => @"
        UPDATE [dbo].[SecretMission]
        SET [EvidenceImageUrl] = @EvidenceImageUrl,
            [EvidenceVideoUrl] = @EvidenceVideoUrl,
            [SubmittedBy] = @SubmittedBy,
            [SubmittedTime] = @SubmittedTime,
            [ModifiedBy] = 'system-submit-evidence',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [Id] = @Id AND [IsDeleted] = 0;";

    public static string GetOverviewByTeamIdQuery() => @"
        SELECT 
            [Id], 
            [Name], 
            [IsAssigned], 
            CAST(CASE WHEN [EvidenceImageUrl] IS NOT NULL AND [EvidenceImageUrl] <> '[]' THEN 1 ELSE 0 END AS BIT) AS HasImageEvidence,
            CAST(CASE WHEN [EvidenceVideoUrl] IS NOT NULL AND [EvidenceVideoUrl] <> '[]' THEN 1 ELSE 0 END AS BIT) AS HasVideoEvidence
        FROM [dbo].[SecretMission]
        WHERE [TeamId] = @TeamId AND [IsDeleted] = 0
        ORDER BY [CreatedAt] DESC;";

    public static string GetDetailByIdAndTeamIdQuery() => @"
        SELECT 
            [Id], 
            [Name], 
            [Description], 
            [IsAssigned], 
            [EvidenceImageUrl] AS EvidenceImageUrlsJson,
            [EvidenceVideoUrl] AS EvidenceVideoUrlsJson,
            [SubmittedTime]
        FROM [dbo].[SecretMission]
        WHERE [Id] = @Id AND [TeamId] = @TeamId AND [IsDeleted] = 0;";
}