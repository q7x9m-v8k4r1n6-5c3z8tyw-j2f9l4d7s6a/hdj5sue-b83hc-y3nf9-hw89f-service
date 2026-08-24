using MediatR;

namespace OVCMOVE2026.Plugin.CQRS.Commands.UpdateSecretMission;

public sealed record UpdateSecretMissionCommand(
    Guid MissionId,
    Guid TeamId,
    string Name,
    string Description
) : IRequest<UpdateSecretMissionResult>;

public class UpdateSecretMissionResult
{
    public bool IsSuccess { get; set; }
    public bool IsConflict { get; set; }
    public bool IsNotFound { get; set; }
    public string Message { get; set; } = string.Empty;
}