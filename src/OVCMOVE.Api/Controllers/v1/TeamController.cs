using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

public class TeamController : BaseController<TeamController>
{
    public TeamController(ILogger<TeamController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    // Task View Teams
    [HttpGet]
    public async Task<IActionResult> GetAllTeams([FromQuery] GetAllTeamsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Nếu frontend không truyền lên thì mặc định dùng object đã khởi tạo sẵn Page=1, PageSize=20
            query ??= new GetAllTeamsQuery();

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(new ApiResponseModel<PagedResult<GetAllTeamsResultModel>>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
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

            return Ok(new ApiResponseModel<List<SearchTeamResultModel>>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing SearchTeams: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}