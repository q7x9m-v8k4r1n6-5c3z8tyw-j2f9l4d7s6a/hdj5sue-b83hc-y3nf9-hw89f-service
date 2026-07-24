namespace OVCMOVE.Domain.Constants;

public static class RaceConstants
{
    public static class RaceStatus
    {
        public const string Draft = "draft";
        public const string Ready = "ready";
        public const string Ongoing = "ongoing";
        public const string Paused = "paused";
        public const string Completed = "completed";
    }
    public static class TeamStatus
    {
        public const string Active = "active";
        public const string Inactive = "inactive";
    }
    public static class OrganizerStatus
    {
        public const string Active = "active";
        public const string Inactive = "inactive";
    }
}
