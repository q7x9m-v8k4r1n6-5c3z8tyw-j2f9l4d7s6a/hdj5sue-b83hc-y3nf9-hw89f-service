namespace OVCMOVE.Domain.Constants;

public static class BoothConstants
{
    public static class BoothType
    {
        public const string Other = "other";
        public const string Intellectual = "intellectual";
        public const string Physical = "physical";

        public static bool IsSupported(string? value) =>
            value is Other or Intellectual or Physical;
    }

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
