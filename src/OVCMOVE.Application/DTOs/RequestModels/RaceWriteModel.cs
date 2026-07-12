namespace OVCMOVE.Application.DTOs.RequestModels;

public class RaceWriteModel
{
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string ImageName { get; init; } = string.Empty;
    public IReadOnlyCollection<RaceStationWriteModel> Stations { get; init; } = Array.Empty<RaceStationWriteModel>();
    public IReadOnlyCollection<RaceNameMissionWriteModel> Teams { get; init; } = Array.Empty<RaceNameMissionWriteModel>();
    public IReadOnlyCollection<RaceNameMissionWriteModel> Organizers { get; init; } = Array.Empty<RaceNameMissionWriteModel>();
    public IReadOnlyCollection<RaceNameMissionWriteModel> SettingsRows { get; init; } = Array.Empty<RaceNameMissionWriteModel>();
}

public class RaceStationWriteModel
{
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Manager { get; init; } = string.Empty;
    public string Points { get; init; } = string.Empty;
}

public class RaceNameMissionWriteModel
{
    public string Name { get; init; } = string.Empty;
    public string Mission { get; init; } = string.Empty;
}
