namespace OVCMOVE.Infrastructure.Options;

public class LoginRateLimitConfigOptions
{
    public const string SectionName = "LoginRateLimitConfig";
    public int MaxFailedAttemptsBeforeWait { get; set; } = 5;
    public int MaxFailedAttemptsBeforeBan { get; set; } = 21;
    public int BaseWaitTimeSeconds { get; set; } = 15;
    public int WaitTimeMultiplier { get; set; } = 2;
}