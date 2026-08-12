using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OVCMOVE.Infrastructure.Persistence.Dapper; // Chứa IDbExecutor
using OVCMOVE2026.Plugin.Models.DTOs;
using OVCMOVE2026.Plugin.Repositories.Queries;

namespace OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionOverview;

// Kiện hàng Query
public sealed record GetSecretMissionOverviewQuery(Guid TeamId, Guid RaceId) : IRequest<List<SecretMissionOverviewDto>>;

// Handler xử lý Query
public class GetSecretMissionOverviewQueryHandler : IRequestHandler<GetSecretMissionOverviewQuery, List<SecretMissionOverviewDto>>
{
    private readonly IDbExecutor _db;

    public GetSecretMissionOverviewQueryHandler(IDbExecutor db)
    {
        _db = db;
    }

    public async Task<List<SecretMissionOverviewDto>> Handle(GetSecretMissionOverviewQuery request, CancellationToken cancellationToken)
    {
        var result = await _db.QueryAsync<SecretMissionOverviewDto>(
            SecretMissionQueries.GetOverviewByTeamIdQuery(),
            new { request.TeamId, request.RaceId },
            cancellationToken: cancellationToken);

        return result.ToList();
    }
}