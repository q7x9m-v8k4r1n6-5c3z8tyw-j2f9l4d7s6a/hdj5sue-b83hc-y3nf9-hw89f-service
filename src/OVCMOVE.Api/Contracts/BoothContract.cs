using System.ComponentModel.DataAnnotations;

namespace OVCMOVE.Api.Contracts;

public class BoothContract
{
    public class GetBoothSummaryRequest
    {
        [Required(ErrorMessage = "thiếu RaceId để lấy trạng thái các booth")]
        public Guid RaceId { get; set; }
    }

    public class GetBoothSummaryResponse
    {
        public Guid BoothId { get; set; }
        public string BoothName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class GetBoothDetailResponse
    {
        public Guid BoothId { get; set; }
        public string BoothName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public string? CurrentTeamName { get; set; } 
    }
}