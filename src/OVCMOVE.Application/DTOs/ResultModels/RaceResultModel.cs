namespace OVCMOVE.Application.DTOs.ResultModels;

public static class RaceResultModel
{
    public sealed class AdminRaceListResultModel
    {
        public DateTime GeneratedAt { get; init; }
        public int TotalCount { get; init; }
        public RaceStatusSummaryResultModel Summary { get; init; } = new();
        public IReadOnlyCollection<AdminRaceListItemResultModel> Items { get; init; } = Array.Empty<AdminRaceListItemResultModel>();
    }

    public sealed class RaceStatusSummaryResultModel
    {
        public int UpcomingCount { get; init; }
        public int InProgressCount { get; init; }
        public int CompletedCount { get; init; }
    }

    public sealed class AdminRaceListItemResultModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public int DurationMinutes { get; init; }
        public string Status { get; init; } = string.Empty;
        public int ParticipantCount { get; init; }
        public int CompletedCheckpoints { get; init; }
        public int PendingCheckpoints { get; init; }
        public DateTime LastUpdatedAt { get; init; }
    }

    public sealed class AdminRaceDetailResultModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public int DurationMinutes { get; init; }
        public string Status { get; init; } = string.Empty;
        public int ParticipantCount { get; init; }
        public string ImageName { get; init; } = string.Empty;
        public DateTime LastUpdatedAt { get; init; }
        public RaceStatisticsResultModel Statistics { get; init; } = new();
        public IReadOnlyCollection<RaceStationResultModel> Stations { get; init; } = Array.Empty<RaceStationResultModel>();
        public IReadOnlyCollection<RaceNameMissionResultModel> Teams { get; init; } = Array.Empty<RaceNameMissionResultModel>();
        public IReadOnlyCollection<RaceNameMissionResultModel> Organizers { get; init; } = Array.Empty<RaceNameMissionResultModel>();
        public IReadOnlyCollection<RaceNameMissionResultModel> SettingsRows { get; init; } = Array.Empty<RaceNameMissionResultModel>();
    }

    public sealed class RaceStatisticsResultModel
    {
        public DateTime GeneratedAt { get; init; }
        public int TotalStations { get; init; }
        public int ActiveStations { get; init; }
        public int CompletedCheckpoints { get; init; }
        public int PendingCheckpoints { get; init; }
        public int TotalTeams { get; init; }
        public int TeamsCompleted { get; init; }
        public int TeamsInProgress { get; init; }
        public int TeamsWaiting { get; init; }
        public int CompletionRate { get; init; }
    }

    public sealed class RaceStationResultModel
    {
        public string Name { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public string Manager { get; init; } = string.Empty;
        public string Points { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    public sealed class RaceNameMissionResultModel
    {
        public string Name { get; init; } = string.Empty;
        public string Mission { get; init; } = string.Empty;
    }
}
