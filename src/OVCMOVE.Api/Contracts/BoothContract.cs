using System.ComponentModel.DataAnnotations;

namespace OVCMOVE.Api.Contracts;

public static class BoothContract
{
    public sealed class SubmitScoreRequest
    {
        [Required]
        public Guid TeamId { get; init; }

        [Required]
        public Guid BoothId { get; init; }

        [Range(0, 100)]
        public int Score { get; init; }
    }

    public sealed class EntryRequest
    {
        [Required]
        public Guid BoothId { get; init; }
    }

    public sealed class AcceptEntryRequest
    {
        [Required]
        public Guid BoothId { get; init; }

        [Required]
        public Guid TeamId { get; init; }
    }

    public sealed class RejectEntryRequest
    {
        [Required]
        public Guid BoothId { get; init; }

        [Required]
        public Guid TeamId { get; init; }
    }

    public sealed record OperationResponse(string Message);

    public sealed record MyBoothResponse
    {
        public Guid BoothId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Place { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public Guid? TeamId { get; init; }
        public string? TeamName { get; init; }
    }
}
