using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Teams.Query.GetAllTeams;

public class GetAllTeamsQueryHandler :
    BaseQueryHandler<GetAllTeamsQueryHandler>,
    IRequestHandler<GetAllTeamsQuery, PagedResult<GetAllTeamsResultModel>>
{
    private readonly ITeamRepository _teamRepository;

    public GetAllTeamsQueryHandler(
        ILogger<GetAllTeamsQueryHandler> logger,
        ITeamRepository teamRepository) : base(logger)
    {
        _teamRepository = teamRepository;
    }

    public async Task<PagedResult<GetAllTeamsResultModel>> Handle(GetAllTeamsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allTeams = await _teamRepository.GetAllAsync(cancellationToken);
            var totalItems = allTeams.Count;
            var pageIndex = Math.Max(1, request.PageIndex);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var pagedTeams = allTeams
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList(); 

            return new PagedResult<GetAllTeamsResultModel>
            {
                Items = pagedTeams,
                TotalItems = totalItems,
                Page = pageIndex,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while handling GetAllTeamsQuery: {Message}", ex.Message);
            throw;
        }
    }
}