using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.RequestModels;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Infrastructure.Repositories;

public class RaceRepository : IRaceRepository
{
    private readonly ILogger<RaceRepository> _logger;
    private readonly Lock _syncRoot = new();
    private readonly List<RaceState> _races;

    public RaceRepository(ILogger<RaceRepository> logger)
    {
        _logger = logger;
        _races = BuildSeedRaces();
    }

    public Task<RaceResultModel.AdminRaceListResultModel> GetAdminRaceListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var generatedAt = DateTime.Now;
            var items = _races
                .OrderByDescending(race => race.StartAt)
                .Select(race => BuildListItem(race, generatedAt))
                .ToArray();

            return Task.FromResult(new RaceResultModel.AdminRaceListResultModel
            {
                GeneratedAt = generatedAt,
                TotalCount = items.Length,
                Summary = new RaceResultModel.RaceStatusSummaryResultModel
                {
                    UpcomingCount = items.Count(item => item.Status == RaceStatuses.Upcoming),
                    InProgressCount = items.Count(item => item.Status == RaceStatuses.Live),
                    CompletedCount = items.Count(item => item.Status == RaceStatuses.Done)
                },
                Items = items
            });
        }
    }

    public Task<RaceResultModel.AdminRaceDetailResultModel?> GetAdminRaceDetailAsync(int raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var race = _races.FirstOrDefault(item => item.Id == raceId);
            return Task.FromResult(race is null ? null : BuildDetailModel(race, DateTime.Now));
        }
    }

    public Task<RaceResultModel.AdminRaceDetailResultModel> CreateAsync(RaceWriteModel race, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var nextId = _races.Count == 0 ? 1 : _races.Max(item => item.Id) + 1;
            var state = BuildState(nextId, race);
            _races.Add(state);
            _logger.LogInformation("Created race {RaceId} - {RaceName}", state.Id, state.Name);
            return Task.FromResult(BuildDetailModel(state, DateTime.Now));
        }
    }

    public Task<RaceResultModel.AdminRaceDetailResultModel?> UpdateAsync(int raceId, RaceWriteModel race, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            var index = _races.FindIndex(item => item.Id == raceId);
            if (index < 0)
            {
                return Task.FromResult<RaceResultModel.AdminRaceDetailResultModel?>(null);
            }

            var updatedState = BuildState(raceId, race);
            _races[index] = updatedState;
            _logger.LogInformation("Updated race {RaceId} - {RaceName}", updatedState.Id, updatedState.Name);
            return Task.FromResult<RaceResultModel.AdminRaceDetailResultModel?>(BuildDetailModel(updatedState, DateTime.Now));
        }
    }

    private static RaceResultModel.AdminRaceListItemResultModel BuildListItem(RaceState race, DateTime generatedAt)
    {
        var statistics = BuildStatistics(race, generatedAt);

        return new RaceResultModel.AdminRaceListItemResultModel
        {
            Id = race.Id,
            Name = race.Name,
            Location = race.Location,
            StartAt = race.StartAt,
            EndAt = race.EndAt,
            DurationMinutes = race.DurationMinutes,
            Status = ResolveStatus(race.StartAt, race.EndAt, generatedAt),
            ParticipantCount = race.Teams.Count,
            CompletedCheckpoints = statistics.CompletedCheckpoints,
            PendingCheckpoints = statistics.PendingCheckpoints,
            LastUpdatedAt = race.LastUpdatedAt
        };
    }

    private static RaceResultModel.AdminRaceDetailResultModel BuildDetailModel(RaceState race, DateTime generatedAt)
    {
        var status = ResolveStatus(race.StartAt, race.EndAt, generatedAt);
        var statistics = BuildStatistics(race, generatedAt);

        return new RaceResultModel.AdminRaceDetailResultModel
        {
            Id = race.Id,
            Name = race.Name,
            Location = race.Location,
            StartAt = race.StartAt,
            EndAt = race.EndAt,
            DurationMinutes = race.DurationMinutes,
            Status = status,
            ParticipantCount = race.Teams.Count,
            ImageName = race.ImageName,
            LastUpdatedAt = race.LastUpdatedAt,
            Statistics = statistics,
            Stations = race.Stations
                .Select((station, index) => new RaceResultModel.RaceStationResultModel
                {
                    Name = station.Name,
                    Location = station.Location,
                    Manager = station.Manager,
                    Points = station.Points,
                    Status = ResolveStationStatus(status, index, race.Stations.Count)
                })
                .ToArray(),
            Teams = race.Teams
                .Select(team => new RaceResultModel.RaceNameMissionResultModel
                {
                    Name = team.Name,
                    Mission = team.Mission
                })
                .ToArray(),
            Organizers = race.Organizers
                .Select(organizer => new RaceResultModel.RaceNameMissionResultModel
                {
                    Name = organizer.Name,
                    Mission = organizer.Mission
                })
                .ToArray(),
            SettingsRows = race.SettingsRows
                .Select(setting => new RaceResultModel.RaceNameMissionResultModel
                {
                    Name = setting.Name,
                    Mission = setting.Mission
                })
                .ToArray()
        };
    }

    private static RaceResultModel.RaceStatisticsResultModel BuildStatistics(RaceState race, DateTime generatedAt)
    {
        var totalStations = race.Stations.Count;
        var totalTeams = race.Teams.Count;
        var status = ResolveStatus(race.StartAt, race.EndAt, generatedAt);

        var completedCheckpoints = status switch
        {
            RaceStatuses.Done => totalStations,
            RaceStatuses.Live => Math.Min(totalStations, Math.Max(1, (int)Math.Round(totalStations * 0.65m))),
            _ => 0
        };

        var pendingCheckpoints = Math.Max(totalStations - completedCheckpoints, 0);
        var teamsCompleted = status switch
        {
            RaceStatuses.Done => totalTeams,
            RaceStatuses.Live => Math.Min(totalTeams, (int)Math.Round(totalTeams * 0.45m)),
            _ => 0
        };
        var teamsWaiting = status == RaceStatuses.Upcoming ? totalTeams : 0;
        var teamsInProgress = Math.Max(totalTeams - teamsCompleted - teamsWaiting, 0);
        var activeStations = status switch
        {
            RaceStatuses.Done => totalStations,
            RaceStatuses.Live => totalStations == 0 ? 0 : Math.Min(totalStations, Math.Max(1, totalStations - 1)),
            _ => 0
        };

        return new RaceResultModel.RaceStatisticsResultModel
        {
            GeneratedAt = generatedAt,
            TotalStations = totalStations,
            ActiveStations = activeStations,
            CompletedCheckpoints = completedCheckpoints,
            PendingCheckpoints = pendingCheckpoints,
            TotalTeams = totalTeams,
            TeamsCompleted = teamsCompleted,
            TeamsInProgress = teamsInProgress,
            TeamsWaiting = teamsWaiting,
            CompletionRate = totalStations == 0 ? 0 : (int)Math.Round((decimal)completedCheckpoints / totalStations * 100m)
        };
    }

    private static string ResolveStatus(DateTime startAt, DateTime endAt, DateTime now)
    {
        if (endAt < now)
        {
            return RaceStatuses.Done;
        }

        if (startAt <= now && endAt >= now)
        {
            return RaceStatuses.Live;
        }

        return RaceStatuses.Upcoming;
    }

    private static string ResolveStationStatus(string raceStatus, int index, int stationCount)
    {
        if (raceStatus == RaceStatuses.Done)
        {
            return "Completed";
        }

        if (raceStatus == RaceStatuses.Live)
        {
            var liveCutoff = stationCount == 0 ? 0 : Math.Max(1, (int)Math.Round(stationCount * 0.65m));
            return index < liveCutoff ? "Active" : "Queued";
        }

        return "Waiting";
    }

    private static RaceState BuildState(int raceId, RaceWriteModel race)
    {
        var trimmedStations = race.Stations
            .Select(station => new RaceStationState(
                Sanitize(station.Name),
                Sanitize(station.Location),
                Sanitize(station.Manager),
                Sanitize(station.Points)))
            .Where(station => !string.IsNullOrWhiteSpace(station.Name) || !string.IsNullOrWhiteSpace(station.Location) || !string.IsNullOrWhiteSpace(station.Manager) || !string.IsNullOrWhiteSpace(station.Points))
            .ToArray();

        var trimmedTeams = race.Teams
            .Select(row => new RaceNameMissionState(Sanitize(row.Name), Sanitize(row.Mission)))
            .Where(row => !string.IsNullOrWhiteSpace(row.Name) || !string.IsNullOrWhiteSpace(row.Mission))
            .ToArray();

        var trimmedOrganizers = race.Organizers
            .Select(row => new RaceNameMissionState(Sanitize(row.Name), Sanitize(row.Mission)))
            .Where(row => !string.IsNullOrWhiteSpace(row.Name) || !string.IsNullOrWhiteSpace(row.Mission))
            .ToArray();

        var trimmedSettings = race.SettingsRows
            .Select(row => new RaceNameMissionState(Sanitize(row.Name), Sanitize(row.Mission)))
            .Where(row => !string.IsNullOrWhiteSpace(row.Name) || !string.IsNullOrWhiteSpace(row.Mission))
            .ToArray();

        return new RaceState(
            raceId,
            Sanitize(race.Name),
            Sanitize(race.Location),
            race.StartAt,
            race.EndAt,
            Math.Max(15, (int)Math.Round((race.EndAt - race.StartAt).TotalMinutes)),
            Sanitize(race.ImageName),
            trimmedStations,
            trimmedTeams,
            trimmedOrganizers,
            trimmedSettings,
            DateTime.Now);
    }

    private static string Sanitize(string? value) => value?.Trim() ?? string.Empty;

    private static List<RaceState> BuildSeedRaces()
    {
        var year = DateTime.Now.Year;

        return
        [
            CreateSeedRace(
                1,
                "Spring Hackathon 2026",
                "Main Hall",
                new DateTime(year, 3, 15, 8, 0, 0),
                new DateTime(year, 3, 17, 18, 0, 0),
                [
                    new RaceStationState("Tram 1", "Toa B6", "Olivia Rhye", "10"),
                    new RaceStationState("Tram 2", "San sau Toa A4", "Phoenix Baker", "10"),
                    new RaceStationState("Tram 3", "Thu vien trung tam", "Linh Tran", "15")
                ],
                [
                    new RaceNameMissionState("Doi 1", "Giai ma checkpoint mo dau"),
                    new RaceNameMissionState("Doi 2", "Vuot chuong ngai vat tai san trung tam"),
                    new RaceNameMissionState("Doi 3", "Hoan thanh duong dua tri tue")
                ],
                [
                    new RaceNameMissionState("Ban dieu phoi", "Dieu phoi tong the va xu ly su co"),
                    new RaceNameMissionState("Ban ky thuat", "Cap nhat bang diem va xu ly he thong")
                ],
                [
                    new RaceNameMissionState("Leaderboard", "An bang xep hang trong 30 phut dau"),
                    new RaceNameMissionState("Checkpoint bonus", "Cong 5 diem cho doi den dung gio")
                ]),
            CreateSeedRace(
                2,
                "Volunteer Marathon 2026",
                "Campus Yard",
                new DateTime(year, 8, 21, 9, 0, 0),
                new DateTime(year, 8, 21, 17, 30, 0),
                [
                    new RaceStationState("Tram xuat phat", "Cong chinh", "Theo Nguyen", "5"),
                    new RaceStationState("Tram tiep suc", "San co dong", "Savannah Nguyen", "10")
                ],
                [
                    new RaceNameMissionState("Blue Wave", "Hoan thanh cac tram tinh nguyen"),
                    new RaceNameMissionState("Campus Hero", "Thu thap day du dau moc")
                ],
                [
                    new RaceNameMissionState("Ban hau can", "Chuan bi vat pham cho doi dua"),
                    new RaceNameMissionState("Ban an ninh", "Dam bao luong di chuyen")
                ],
                [
                    new RaceNameMissionState("Broadcast", "Mo kenh thong bao truc tuyen"),
                    new RaceNameMissionState("Medical", "Bo tri tram y te co dinh")
                ]),
            CreateSeedRace(
                3,
                "MOVE Campus Relay",
                "Innovation Center",
                new DateTime(year, 9, 5, 7, 30, 0),
                new DateTime(year, 9, 5, 12, 0, 0),
                [
                    new RaceStationState("Tram sang tao", "Tang 2 nha E", "Jenny Wilson", "10"),
                    new RaceStationState("Tram toc do", "San truoc nha D", "Wade Warren", "20"),
                    new RaceStationState("Tram dich", "Hoi truong lon", "Kristin Watson", "15")
                ],
                [
                    new RaceNameMissionState("Innovation Crew", "Giai ma chuoi nhiem vu sang tao"),
                    new RaceNameMissionState("Relay Force", "Vuot tram toc do va ve dich an toan"),
                    new RaceNameMissionState("North Star", "Tich luy diem thuong o moi checkpoint"),
                    new RaceNameMissionState("Compass", "Giu nhom day du thanh vien tai moi tram")
                ],
                [
                    new RaceNameMissionState("Ban quan tro", "Giam sat lich trinh va ket qua"),
                    new RaceNameMissionState("Ban truyen thong", "Cap nhat thong tin thoi gian thuc")
                ],
                [
                    new RaceNameMissionState("Notifications", "Gui thong bao khi doi hoan thanh checkpoint"),
                    new RaceNameMissionState("Penalty rule", "Tru diem doi bo lo checkpoint")
                ])
        ];
    }

    private static RaceState CreateSeedRace(
        int id,
        string name,
        string location,
        DateTime startAt,
        DateTime endAt,
        IReadOnlyCollection<RaceStationState> stations,
        IReadOnlyCollection<RaceNameMissionState> teams,
        IReadOnlyCollection<RaceNameMissionState> organizers,
        IReadOnlyCollection<RaceNameMissionState> settingsRows)
    {
        return new RaceState(
            id,
            name,
            location,
            startAt,
            endAt,
            Math.Max(15, (int)Math.Round((endAt - startAt).TotalMinutes)),
            string.Empty,
            stations.ToArray(),
            teams.ToArray(),
            organizers.ToArray(),
            settingsRows.ToArray(),
            DateTime.Now.AddMinutes(-id * 9));
    }

    private sealed record RaceState(
        int Id,
        string Name,
        string Location,
        DateTime StartAt,
        DateTime EndAt,
        int DurationMinutes,
        string ImageName,
        IReadOnlyCollection<RaceStationState> Stations,
        IReadOnlyCollection<RaceNameMissionState> Teams,
        IReadOnlyCollection<RaceNameMissionState> Organizers,
        IReadOnlyCollection<RaceNameMissionState> SettingsRows,
        DateTime LastUpdatedAt);

    private sealed record RaceStationState(string Name, string Location, string Manager, string Points);

    private sealed record RaceNameMissionState(string Name, string Mission);

    private static class RaceStatuses
    {
        public const string Upcoming = "upcoming";
        public const string Live = "live";
        public const string Done = "done";
    }
}
