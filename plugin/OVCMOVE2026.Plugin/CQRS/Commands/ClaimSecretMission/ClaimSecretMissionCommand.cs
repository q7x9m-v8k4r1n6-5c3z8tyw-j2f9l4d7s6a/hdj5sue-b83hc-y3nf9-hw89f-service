using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Commands.ClaimSecretMission;

// 1. Kiện hàng vận chuyển
public sealed record ClaimSecretMissionCommand(Guid MissionId, Guid TeamId) : IRequest<ClaimSecretMissionResult>;

// 2. Object định tuyến HTTP Status Code
public class ClaimSecretMissionResult
{
    public bool IsSuccess { get; set; }
    public bool IsConflict { get; set; }
    public bool IsNotFound { get; set; }
    public string Message { get; set; } = string.Empty;
}

// 3. Bộ não xử lý 3 States
public class ClaimSecretMissionCommandHandler : IRequestHandler<ClaimSecretMissionCommand, ClaimSecretMissionResult>
{
    private readonly ISecretMissionRepository _repository;

    public ClaimSecretMissionCommandHandler(ISecretMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClaimSecretMissionResult> Handle(ClaimSecretMissionCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        
        if (mission == null)
        {
            return new ClaimSecretMissionResult { IsNotFound = true, Message = "Không tìm thấy nhiệm vụ bí mật này." };
        }

        // KIỂM TRA STATE
        if (mission.IsAssigned)
        {
            if (mission.TeamId == request.TeamId)
            {
                // State 2: Lỡ quét lại (Trả về Success để FE đi tiếp)
                return new ClaimSecretMissionResult { IsSuccess = true, Message = "Đội của bạn đã nhận nhiệm vụ này rồi." };
            }
            else
            {
                // State 3: Bị cướp (Trả về Conflict)
                return new ClaimSecretMissionResult { IsConflict = true, Message = "Rất tiếc, hộp mù này đã bị đội khác tìm thấy và nhận mất!" };
            }
        }

        // State 1: Happy Path (Chưa ai nhận)
        mission.IsAssigned = true;
        mission.TeamId = request.TeamId;
        mission.ReceivedBy = request.TeamId; // Vì TeamId và UserId là một
        mission.ReceivedTime = DateTime.UtcNow;

        await _repository.UpdateClaimAsync(mission, cancellationToken);

        return new ClaimSecretMissionResult { IsSuccess = true, Message = "Nhận nhiệm vụ bí mật thành công!" };
    }
}