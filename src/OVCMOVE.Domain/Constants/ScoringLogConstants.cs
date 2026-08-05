namespace OVCMOVE.Domain.Constants;

public static class ScoringLogConstants
{
    public static class ReasonCode
    {
        public const string BoothCompleted = "BOOTH_COMPLETED";
        public const string Manual = "manual";
    }

    public static class Source
    {
        public const string BoothCompleted = "booth_completed";
        public const string AdminFix = "admin_fix";
    }
}
