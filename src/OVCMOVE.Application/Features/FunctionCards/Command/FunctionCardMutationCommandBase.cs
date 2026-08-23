using System.Text.Json;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Command;

public abstract class FunctionCardMutationCommandBase : AuditedRequest
{
    public string CardKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? BackgroundUrl { get; init; }
    public JsonElement Inputs { get; init; }
}