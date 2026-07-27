using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.SearchTeam;

public class SearchTeamQueryHandler :
    IRequestHandler<SearchTeamQuery, IReadOnlyCollection<SearchTeamResultModel>>
{
    private readonly ITeamRepository _teamRepository;

    public SearchTeamQueryHandler(
        ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    /// <summary>Searches team accounts and maps them to the feature result.</summary>
    public async Task<IReadOnlyCollection<SearchTeamResultModel>> Handle(
        SearchTeamQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ApplicationValidationException(
                "Từ khóa tìm kiếm không được để trống.");
        }

        var teams = await _teamRepository.SearchAsync(
            request.Keyword.Trim(),
            cancellationToken);
        return teams.Select(MapTeam).ToArray();
    }

    private static SearchTeamResultModel MapTeam(User user) => new()
    {
        Id = user.Id,
        Name = string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.Username ?? user.LinkedEmail
            : user.DisplayName,
        LeaderEmail = user.LinkedEmail
    };
}
