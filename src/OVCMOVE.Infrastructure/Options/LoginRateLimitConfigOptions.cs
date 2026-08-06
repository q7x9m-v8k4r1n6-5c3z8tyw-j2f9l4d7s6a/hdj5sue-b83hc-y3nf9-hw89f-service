namespace OVCMOVE.Infrastructure.Options;

public class LoginRateLimitConfigOptions
{
    public const string SectionName = "LoginRateLimitConfig";
    public int MaxFailedAttemptsBeforeWait { get; set; }
    public int MaxFailedAttemptsBeforeBan { get; set; }
    public int BaseWaitTimeSeconds { get; set; }
    public int WaitTimeMultiplier { get; set; }
}