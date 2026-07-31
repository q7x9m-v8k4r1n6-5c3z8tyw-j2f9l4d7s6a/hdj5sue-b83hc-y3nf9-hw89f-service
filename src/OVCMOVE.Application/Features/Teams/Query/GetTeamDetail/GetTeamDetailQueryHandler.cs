using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Teams.Query.GetTeamDetail;

public sealed class GetTeamDetailQueryHandler :
    IRequestHandler<GetTeamDetailQuery, User?>
{
    private readonly ITeamRepository _teams;

    public GetTeamDetailQueryHandler(ITeamRepository teams) => _teams = teams;

    public Task<User?> Handle(
        GetTeamDetailQuery request,
        CancellationToken cancellationToken) =>
        _teams.GetByIdAsync(request.TeamId, cancellationToken);
}
