using MediatR;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Commands.UpdateSecretMission;

public class UpdateSecretMissionCommandHandler
    : IRequestHandler<UpdateSecretMissionCommand, UpdateSecretMissionResult>
{
    private readonly ISecretMissionRepository _repository;

    public UpdateSecretMissionCommandHandler(ISecretMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateSecretMissionResult> Handle(
        UpdateSecretMissionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new UpdateSecretMissionResult { IsSuccess = false, Message = "Tên nhiệm vụ không được để trống." };
        }

        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission == null)
        {
            return new UpdateSecretMissionResult { IsNotFound = true, Message = "Không tìm thấy nhiệm vụ bí mật này." };
        }

        // Nếu đổi sang 1 đội khác đội hiện tại, kiểm tra đội mới có đang giữ NVBM khác không.
        if (mission.TeamId != request.TeamId)
        {
            var alreadyHasMission = await _repository.HasAssignedMissionForTeamAsync(
                mission.RaceId!.Value,
                request.TeamId,
                cancellationToken,
                excludeMissionId: request.MissionId);
            if (alreadyHasMission)
            {
                return new UpdateSecretMissionResult
                {
                    IsConflict = true,
                    Message = "Đội này đã được gán một nhiệm vụ bí mật khác trong trận đấu này.",
                };
            }
        }

        await _repository.UpdateMissionAsync(
            request.MissionId,
            request.TeamId,
            request.Name.Trim(),
            request.Description?.Trim() ?? string.Empty,
            cancellationToken);

        return new UpdateSecretMissionResult { IsSuccess = true, Message = "Cập nhật nhiệm vụ thành công!" };
    }
}