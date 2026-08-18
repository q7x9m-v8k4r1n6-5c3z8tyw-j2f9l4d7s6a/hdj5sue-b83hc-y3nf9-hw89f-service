using System.Text.Json;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.FunctionCards.Common;

public static class FunctionCardConstants
{
    public static class Category
    {
        public const string Attack = "attack";
        public const string Defense = "defense";
        public const string Effect = "effect";
    }
}

public sealed record FunctionCardResultModel(
    Guid Id,
    Guid RaceId,
    Guid? TeamId,
    string? TeamName,
    string CardKey,
    string Name,
    string Description,
    string Category,
    string? BackgroundUrl,
    JsonElement Inputs,
    Guid? WorkflowId,
    string? WorkflowName,
    string? WorkflowStatus,
    DateTime CreatedAt,
    DateTime ModifiedAt);

public sealed class FunctionCardReadRow
{
    public Guid Id { get; init; }
    public Guid RaceId { get; init; }
    public Guid? TeamId { get; init; }
    public string? TeamName { get; init; }
    public string CardKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? BackgroundUrl { get; init; }
    public string InputsJson { get; init; } = "[]";
    public Guid? WorkflowId { get; init; }
    public string? WorkflowName { get; init; }
    public string? WorkflowStatus { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }

    public FunctionCardResultModel ToResult() => new(
        Id, RaceId, TeamId, TeamName, CardKey, Name, Description, Category,
        BackgroundUrl, ParseInputs(InputsJson), WorkflowId, WorkflowName,
        WorkflowStatus, CreatedAt, ModifiedAt);

    private static JsonElement ParseInputs(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

public static class FunctionCardMapping
{
    public static FunctionCardResultModel ToResult(
        this FunctionCard card,
        string? teamName = null,
        Guid? workflowId = null,
        string? workflowName = null,
        string? workflowStatus = null) => new FunctionCardReadRow
        {
            Id = card.Id,
            RaceId = card.RaceId,
            TeamId = card.TeamId,
            TeamName = teamName,
            CardKey = card.CardKey,
            Name = card.Name,
            Description = card.Description,
            Category = card.Category,
            BackgroundUrl = card.BackgroundUrl,
            InputsJson = card.InputsJson,
            WorkflowId = workflowId,
            WorkflowName = workflowName,
            WorkflowStatus = workflowStatus,
            CreatedAt = card.CreatedAt,
            ModifiedAt = card.ModifiedAt
        }.ToResult();
}
