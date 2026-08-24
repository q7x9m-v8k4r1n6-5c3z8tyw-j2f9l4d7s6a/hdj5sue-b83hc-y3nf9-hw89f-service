using MediatR;
using OVCMOVE2026.Plugin.Models.DTOs;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionAdminDetail;

public sealed record GetSecretMissionAdminDetailQuery(Guid Id)
    : IRequest<SecretMissionAdminDetailDto?>;

public class GetSecretMissionAdminDetailQueryHandler
    : IRequestHandler<GetSecretMissionAdminDetailQuery, SecretMissionAdminDetailDto?>
{
    private readonly ISecretMissionRepository _repository;

    public GetSecretMissionAdminDetailQueryHandler(ISecretMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<SecretMissionAdminDetailDto?> Handle(
        GetSecretMissionAdminDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetAdminDetailAsync(request.Id, cancellationToken);
    }
}