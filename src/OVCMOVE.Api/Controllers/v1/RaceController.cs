using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Race.Command.CreateRace;
using OVCMOVE.Application.Features.Race.Command.UpdateRace;
using OVCMOVE.Application.Features.Race.Query.GetAdminRaceDetail;
using OVCMOVE.Application.Features.Race.Query.GetAdminRaceList;
using OVCMOVE.Domain.Constants;
using static OVCMOVE.Api.Contracts.RaceContract;

namespace OVCMOVE.Api.Controllers.v1;

public class RaceController : BaseController<RaceController>
{
    public RaceController(ILogger<RaceController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAdminRaces(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminRaceListQuery(), cancellationToken);
            return Ok(new ApiResponseModel<RaceResultModel.AdminRaceListResultModel>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing GetAdminRaces: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpGet("{raceId:int}")]
    public async Task<IActionResult> GetAdminRaceDetail(int raceId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminRaceDetailQuery { RaceId = raceId }, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<RaceResultModel.AdminRaceDetailResultModel>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing GetAdminRaceDetail: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRace([FromBody] UpsertRaceRequestModel requestModel, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<CreateRaceCommand>(requestModel);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponseModel<RaceResultModel.AdminRaceDetailResultModel>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing CreateRace: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPut("{raceId:int}")]
    public async Task<IActionResult> UpdateRace(int raceId, [FromBody] UpsertRaceRequestModel requestModel, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<UpdateRaceCommand>(requestModel);
            command.RaceId = raceId;
            var result = await _mediator.Send(command, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<RaceResultModel.AdminRaceDetailResultModel>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing UpdateRace: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
