using MediatR;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Queries.VerifyMissionCode;

public sealed record VerifyMissionCodeQuery(Guid MissionId, string Code) : IRequest<bool>;

public class VerifyMissionCodeQueryHandler : IRequestHandler<VerifyMissionCodeQuery, bool>
{
    private readonly ISecretMissionRepository _repository;

    public VerifyMissionCodeQueryHandler(ISecretMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(VerifyMissionCodeQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission == null || string.IsNullOrEmpty(mission.AccessCode)) return false;

        return string.Equals(mission.AccessCode.Trim(), request.Code.Trim(), StringComparison.Ordinal);
    }
}