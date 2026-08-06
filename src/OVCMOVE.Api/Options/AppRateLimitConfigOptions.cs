namespace OVCMOVE.Api.Options;

public class AppRateLimitConfigOptions
{
    public const string SectionName = "AppRateLimitConfig";
    public int DefaultRetryAfterSeconds { get; set; }
    public int PermitLimit { get; set; }
    public double WindowMinutes { get; set; }
    public int SegmentsPerWindow { get; set; }
    public int QueueLimit { get; set; }
}