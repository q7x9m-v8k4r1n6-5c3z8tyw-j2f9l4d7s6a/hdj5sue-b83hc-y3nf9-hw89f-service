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
}
