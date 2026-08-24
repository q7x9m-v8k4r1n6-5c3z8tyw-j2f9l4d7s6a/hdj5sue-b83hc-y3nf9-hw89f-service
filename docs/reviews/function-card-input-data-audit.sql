SET NOCOUNT ON;

/* Read-only audit. This script does not update or delete data. */
WITH CardInputs AS
(
    SELECT
        FC.[Id] AS [CardId],
        FC.[RaceId],
        FC.[CardKey],
        J.[key] AS [InputIndex],
        J.[value] AS [InputJson],
        J.[type] AS [InputJsonType]
    FROM [dbo].[FunctionCards] FC
    CROSS APPLY OPENJSON(FC.[InputsJson]) J
    WHERE FC.[IsDeleted] = 0
),
InputKeys AS
(
    SELECT
        CI.*,
        K.[value] AS [InputKey],
        K.[type] AS [InputKeyJsonType]
    FROM CardInputs CI
    OUTER APPLY
    (
        SELECT TOP (1) OJ.[value], OJ.[type]
        FROM OPENJSON(
            CASE WHEN CI.[InputJsonType] = 5
                THEN CI.[InputJson]
                ELSE N'{}'
            END) OJ
        WHERE OJ.[key] = N'key'
    ) K
),
DuplicateKeys AS
(
    SELECT
        IK.[CardId],
        LTRIM(RTRIM(IK.[InputKey])) COLLATE Latin1_General_100_BIN2
            AS [NormalizedInputKey]
    FROM InputKeys IK
    WHERE IK.[InputKeyJsonType] = 1
      AND NULLIF(LTRIM(RTRIM(IK.[InputKey])), N'') IS NOT NULL
    GROUP BY
        IK.[CardId],
        LTRIM(RTRIM(IK.[InputKey])) COLLATE Latin1_General_100_BIN2
    HAVING COUNT(*) > 1
)
SELECT
    IK.[CardId],
    IK.[RaceId],
    IK.[CardKey],
    IK.[InputIndex],
    IK.[InputKey],
    CASE
        WHEN IK.[InputJsonType] <> 5 THEN N'INPUT_NOT_OBJECT'
        WHEN IK.[InputKeyJsonType] IS NULL THEN N'INPUT_KEY_MISSING'
        WHEN IK.[InputKeyJsonType] <> 1 THEN N'INPUT_KEY_NOT_STRING'
        WHEN NULLIF(LTRIM(RTRIM(IK.[InputKey])), N'') IS NULL THEN N'INPUT_KEY_EMPTY'
        WHEN DATALENGTH(IK.[InputKey]) <>
             DATALENGTH(LTRIM(RTRIM(IK.[InputKey])))
            THEN N'INPUT_KEY_OUTER_WHITESPACE'
        WHEN DK.[CardId] IS NOT NULL THEN N'INPUT_KEY_DUPLICATE'
    END AS [Issue]
FROM InputKeys IK
LEFT JOIN DuplicateKeys DK
    ON DK.[CardId] = IK.[CardId]
   AND DK.[NormalizedInputKey] =
       LTRIM(RTRIM(IK.[InputKey])) COLLATE Latin1_General_100_BIN2
WHERE IK.[InputJsonType] <> 5
   OR IK.[InputKeyJsonType] IS NULL
   OR IK.[InputKeyJsonType] <> 1
   OR NULLIF(LTRIM(RTRIM(IK.[InputKey])), N'') IS NULL
   OR DATALENGTH(IK.[InputKey]) <>
      DATALENGTH(LTRIM(RTRIM(IK.[InputKey])))
   OR DK.[CardId] IS NOT NULL
ORDER BY IK.[RaceId], IK.[CardKey], TRY_CONVERT(INT, IK.[InputIndex]);
