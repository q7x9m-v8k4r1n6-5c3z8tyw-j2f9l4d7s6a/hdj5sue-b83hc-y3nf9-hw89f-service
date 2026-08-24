using MediatR;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Commands.CreateSecretMission;

public class CreateSecretMissionCommandHandler
    : IRequestHandler<CreateSecretMissionCommand, CreateSecretMissionResult>
{
    private readonly ISecretMissionRepository _repository;

    public CreateSecretMissionCommandHandler(ISecretMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateSecretMissionResult> Handle(
        CreateSecretMissionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new CreateSecretMissionResult
            {
                IsSuccess = false,
                Message = "Tên nhiệm vụ không được để trống.",
            };
        }

        var alreadyHasMission = await _repository.HasAssignedMissionForTeamAsync(
            request.RaceId,
            request.TeamId,
            cancellationToken);
        if (alreadyHasMission)
        {
            return new CreateSecretMissionResult
            {
                IsSuccess = false,
                IsConflict = true,
                Message = "Đội này đã được gán một nhiệm vụ bí mật khác trong trận đấu này.",
            };
        }

        var now = DateTime.UtcNow;
        var mission = new SecretMission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            RaceId = request.RaceId,
            IsAssigned = true,
            TeamId = request.TeamId,
            ReceivedBy = request.TeamId,
            ReceivedTime = now,
            CreatedAt = now,
            CreatedBy = "admin-create-mission",
            ModifiedAt = now,
            ModifiedBy = "admin-create-mission",
            IsDeleted = false,
        };

        await _repository.CreateAssignedMissionAsync(mission, cancellationToken);

        return new CreateSecretMissionResult
        {
            IsSuccess = true,
            MissionId = mission.Id,
            Message = "Tạo và gán nhiệm vụ bí mật thành công!",
        };
    }
}