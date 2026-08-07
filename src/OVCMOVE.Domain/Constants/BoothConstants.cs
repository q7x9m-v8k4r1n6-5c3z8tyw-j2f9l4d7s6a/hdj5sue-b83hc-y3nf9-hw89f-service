namespace OVCMOVE.Domain.Constants;

public static class BoothConstants
{
    public static class BoothStatus
    {
        public const string Free = "free";
        public const string Pending = "pending";
        public const string Occupied = "occupied";
    }

    public static class ParticipationRule
    {
        public const int RegularBoothsPerHiddenBooth = 2;
        public const int MaximumHiddenBooths = 3;
    }
}
