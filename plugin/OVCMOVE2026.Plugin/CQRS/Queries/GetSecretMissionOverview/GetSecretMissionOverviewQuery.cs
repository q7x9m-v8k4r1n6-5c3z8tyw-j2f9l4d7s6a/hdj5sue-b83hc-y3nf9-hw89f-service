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
public sealed record GetSecretMissionOverviewQuery(Guid TeamId) : IRequest<List<SecretMissionOverviewDto>>;

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
        // Gọi thẳng SQL, Dapper sẽ tự động map 5 cột SQL vào 5 Property của DTO
        var result = await _db.QueryAsync<SecretMissionOverviewDto>(
            SecretMissionQueries.GetOverviewByTeamIdQuery(),
            new { request.TeamId },
            cancellationToken: cancellationToken);

        return result.ToList();
    }
}