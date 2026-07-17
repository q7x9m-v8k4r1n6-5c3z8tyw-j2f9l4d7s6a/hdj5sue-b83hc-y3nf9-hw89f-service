using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;

namespace OVCMOVE.Application.Features.Teams.Query.SearchTeam;

public class SearchTeamQueryHandler :
    BaseQueryHandler<SearchTeamQueryHandler>,
    IRequestHandler<SearchTeamQuery, List<SearchTeamResultModel>>
{
    private readonly IMapper _mapper;
    private readonly ITeamRepository _teamRepository;

    public SearchTeamQueryHandler(
        ILogger<SearchTeamQueryHandler> logger,
        IMapper mapper,
        ITeamRepository teamRepository) : base(logger)
    {
        _mapper = mapper;
        _teamRepository = teamRepository;
    }

    public async Task<List<SearchTeamResultModel>> Handle(SearchTeamQuery request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var teams = await _teamRepository.SearchAsync(request.Keyword, cancellationToken);
            return _mapper.Map<List<SearchTeamResultModel>>(teams);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while handling SearchTeamsQuery: {Message}", ex.Message);
            throw;
        }
    }
}