using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace OVCMOVE.Api.Contracts;

public static class FunctionCardContract
{
    public class MutationRequest
    {
        [Required, MaxLength(100)] public string CardKey { get; init; } = string.Empty;
        [Required, MaxLength(255)] public string Name { get; init; } = string.Empty;
        [MaxLength(1000)] public string Description { get; init; } = string.Empty;
        [Required] public string Category { get; init; } = string.Empty;
        [MaxLength(2048)] public string? BackgroundUrl { get; init; }
        [Required] public JsonElement Inputs { get; init; }
    }

    public sealed class UpdateRequest : MutationRequest
    {
        [Required] public DateTime ExpectedModifiedAt { get; init; }
    }

    public sealed class AssignTeamRequest
    {
        public Guid? TeamId { get; init; }
        [Required] public DateTime ExpectedModifiedAt { get; init; }
    }
}
