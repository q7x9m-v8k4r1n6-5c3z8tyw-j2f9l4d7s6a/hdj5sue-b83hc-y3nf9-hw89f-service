using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Features.Teams.Query.GetAllTeams;

public class GetAllTeamsQueryHandler :
    IRequestHandler<GetAllTeamsQuery, PagedResult<GetAllTeamsResultModel>>
{
    private readonly ITeamRepository _teamRepository;

    public GetAllTeamsQueryHandler(
        ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    /// <summary>Returns one normalized page of team accounts.</summary>
    public async Task<PagedResult<GetAllTeamsResultModel>> Handle(GetAllTeamsQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (page, pageSize) = Pagination.Normalize(
            request.Page,
            request.PageSize);
        var (teams, totalItems) = await _teamRepository.GetPageAsync(
            request.Search,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<GetAllTeamsResultModel>
        {
            Items = teams.Select(MapTeam).ToArray(),
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    private static GetAllTeamsResultModel MapTeam(User user) => new()
    {
        Id = user.Id,
        Name = GetDisplayName(user),
        LeaderEmail = user.LinkedEmail,
        Username = user.Username ?? string.Empty,
        Status = user.Status
    };

    private static string GetDisplayName(User user) =>
        string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.Username ?? user.LinkedEmail
            : user.DisplayName;
}
