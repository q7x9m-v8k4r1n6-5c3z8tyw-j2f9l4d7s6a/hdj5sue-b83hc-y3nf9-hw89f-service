using MediatR;

namespace OVCMOVE2026.Plugin.CQRS.Commands.CreateSecretMission;

public sealed record CreateSecretMissionCommand(
    Guid RaceId,
    Guid TeamId,
    string Name,
    string Description
) : IRequest<CreateSecretMissionResult>;

public class CreateSecretMissionResult
{
    public bool IsSuccess { get; set; }
    public bool IsConflict { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? MissionId { get; set; }
}