using MediatR;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Commands.DeleteSecretMission;

public sealed record DeleteSecretMissionCommand(Guid MissionId) : IRequest<bool>;

public class DeleteSecretMissionCommandHandler : IRequestHandler<DeleteSecretMissionCommand, bool>
{
    private readonly ISecretMissionRepository _repository;

    public DeleteSecretMissionCommandHandler(ISecretMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteSecretMissionCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission == null) return false;

        await _repository.SoftDeleteAsync(request.MissionId, cancellationToken);
        return true;
    }
}