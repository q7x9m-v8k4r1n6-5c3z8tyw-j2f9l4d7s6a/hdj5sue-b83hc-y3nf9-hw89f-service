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
public class UpdateSecretMissionRequest
{
    public Guid TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}