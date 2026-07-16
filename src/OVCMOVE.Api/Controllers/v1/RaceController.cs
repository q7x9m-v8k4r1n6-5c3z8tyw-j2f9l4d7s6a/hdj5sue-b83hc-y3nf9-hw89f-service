using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.UpdateRace;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Application.Features.Races.Query.GetRaceDetail;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

public class RaceController : BaseController<RaceController>
{
    public RaceController(ILogger<RaceController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRaces(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetAllRacesQuery(), cancellationToken);
            return Ok(new ApiResponseModel<IReadOnlyCollection<RaceListItemResultModel>>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing GetAllRaces: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpGet("{raceId:guid}")]
    public async Task<IActionResult> GetRaceDetail(Guid raceId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetRaceDetailQuery { RaceId = raceId }, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(
                    APIContansts.StatusCode.NotFound,
                    APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<RaceDetailResultModel>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing GetRaceDetail: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRace([FromBody] RaceContract.UpsertRaceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<CreateRaceCommand>(request);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponseModel<Guid?>(
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

    [HttpPut("{raceId:guid}")]
    public async Task<IActionResult> UpdateRace(Guid raceId, [FromBody] RaceContract.UpsertRaceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<UpdateRaceCommand>(request);
            command.RaceId = raceId;

            var result = await _mediator.Send(command, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(
                    APIContansts.StatusCode.NotFound,
                    APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<RaceDetailResultModel>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new ApiResponseModel<object>(
                APIContansts.StatusCode.BadRequest,
                APIContansts.StatusMessage.BadRequest,
                detailError: ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing UpdateRace: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
