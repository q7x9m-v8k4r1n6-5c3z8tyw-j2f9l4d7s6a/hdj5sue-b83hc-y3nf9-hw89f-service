namespace OVCMOVE.Api.Contracts;

public static class RaceContract
{
    public sealed class UpsertRaceRequestModel
    {
        public string Name { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public string ImageName { get; init; } = string.Empty;
        public IReadOnlyCollection<RaceStationRequestModel> Stations { get; init; } = Array.Empty<RaceStationRequestModel>();
        public IReadOnlyCollection<RaceNameMissionRequestModel> Teams { get; init; } = Array.Empty<RaceNameMissionRequestModel>();
        public IReadOnlyCollection<RaceNameMissionRequestModel> Organizers { get; init; } = Array.Empty<RaceNameMissionRequestModel>();
        public IReadOnlyCollection<RaceNameMissionRequestModel> SettingsRows { get; init; } = Array.Empty<RaceNameMissionRequestModel>();
    }

    public sealed class RaceStationRequestModel
    {
        public string Name { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public string Manager { get; init; } = string.Empty;
        public string Points { get; init; } = string.Empty;
    }

    public sealed class RaceNameMissionRequestModel
    {
        public string Name { get; init; } = string.Empty;
        public string Mission { get; init; } = string.Empty;
    }
}
