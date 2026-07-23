using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts; 
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;
using OVCMOVE.Application.Features.Teams.Query.GetTeamLeaderboard;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

public class TeamController : BaseController<TeamController>
{
    public TeamController(ILogger<TeamController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    // Task View Teams
    [HttpGet("view-list")]
    public async Task<IActionResult> GetAllTeams([FromQuery] TeamContract.GetTeamsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

      
            var query = _mapper.Map<GetAllTeamsQuery>(request ?? new TeamContract.GetTeamsRequest());

            var result = await _mediator.Send(query, cancellationToken);

            var response = _mapper.Map<PagedResult<TeamContract.GetTeamsResponse>>(result);

            return Ok(new ApiResponseModel<PagedResult<TeamContract.GetTeamsResponse>>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing GetAllTeams: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    // Thanh tìm kiếm Teams
    [HttpGet("search")]
    public async Task<IActionResult> SearchTeams([FromQuery] string query, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _mediator.Send(new SearchTeamQuery(query), cancellationToken);

            var response = _mapper.Map<List<TeamContract.SearchTeamResponse>>(result);

            return Ok(new ApiResponseModel<List<TeamContract.SearchTeamResponse>>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing SearchTeams: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] TeamContract.GetTeamLeaderboardRequest request, CancellationToken cancellationToken)
    {
        var query = _mapper.Map<GetTeamLeaderboardQuery>(request ?? new TeamContract.GetTeamLeaderboardRequest());        var result = await _mediator.Send(query, cancellationToken);
        var response = _mapper.Map<List<TeamContract.GetTeamLeaderboardResponse>>(result);

        return Ok(new ApiResponseModel<List<TeamContract.GetTeamLeaderboardResponse>>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });
    }
}