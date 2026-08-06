namespace OVCMOVE.Domain.Constants;

public static class ScoringLogConstants
{
    public static class EventCode
    {
        public const string Booth = "BOOTH";
        public const string ManualScoreAdjustment = "manual-score-adjustment";
    }

    public static class EventName
    {
        public const string BoothScoring = "Chấm điểm trạm";
        public const string ManualScoreAdjustment = "Điều chỉnh điểm thủ công";
    }

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

    public static class Reason
    {
        public const string BoothCompleted = "Hoàn thành thử thách tại trạm";
    }
}
