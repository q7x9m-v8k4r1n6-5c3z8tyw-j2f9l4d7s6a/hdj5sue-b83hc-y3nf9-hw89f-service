using MediatR;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE2026.Plugin.Models.DTOs;
using OVCMOVE2026.Plugin.Repositories.Queries;

namespace OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionAdminOverview;

public sealed record GetSecretMissionAdminOverviewQuery(Guid RaceId)
    : IRequest<List<SecretMissionAdminOverviewDto>>;

public class GetSecretMissionAdminOverviewQueryHandler
    : IRequestHandler<GetSecretMissionAdminOverviewQuery, List<SecretMissionAdminOverviewDto>>
{
    private readonly IDbExecutor _db;

    public GetSecretMissionAdminOverviewQueryHandler(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<List<SecretMissionAdminOverviewDto>> Handle(
        GetSecretMissionAdminOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _db.QueryAsync<SecretMissionAdminOverviewDto>(
            SecretMissionQueries.GetAdminOverviewByRaceIdQuery(),
            new { request.RaceId },
            cancellationToken: cancellationToken);

        return result.ToList();
    }
}