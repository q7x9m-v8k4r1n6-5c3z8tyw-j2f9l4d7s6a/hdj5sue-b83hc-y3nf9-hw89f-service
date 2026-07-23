using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.Features.Logs.Query.GetBoothScoringLogs;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

public class LogsController : BaseController<LogsController>
{
    public LogsController(ILogger<LogsController> logger, IMediator mediator, IMapper mapper) 
        : base(logger, mediator, mapper)
    {
    }

    // [GET] api/v1/logs/booth-scoring
    [HttpGet("booth-scoring")]
    public async Task<IActionResult> GetBoothScoringLogs([FromQuery] LogsContract.BoothScoringLogsRequest request, CancellationToken cancellationToken)
    {
        var query = _mapper.Map<GetBoothScoringLogsQuery>(request ?? new LogsContract.BoothScoringLogsRequest());
        var result = await _mediator.Send(query, cancellationToken);
        var response = _mapper.Map<List<LogsContract.BoothScoringLogsResponse>>(result);

        return Ok(new ApiResponseModel<List<LogsContract.BoothScoringLogsResponse>>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });
    }
}