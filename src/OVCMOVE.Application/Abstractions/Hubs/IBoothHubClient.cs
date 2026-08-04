namespace OVCMOVE.Application.Abstractions.Hubs;

public interface IBoothHubClient
{
    Task ReceiveBoothStatusChanged(Guid boothId, string status, Guid? teamId, string? teamName);
    Task ReceiveRaceScoreChanged(Guid raceId, Guid teamId, int delta);
}
