using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Application.Features.Races.Query.GetRaceDetail;
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
    public async Task<IActionResult> GetAllRaces([FromQuery] GetAllRacesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = _mapper.Map<GetAllRacesQuery>(request);

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(new ApiResponseModel<PagedResult<RaceItemResultModel>>(
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

    [HttpGet("{raceId}")]
    public async Task<IActionResult> GetRaceDetail([FromQuery] Guid raceId, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

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
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> CreateRace([FromBody] RaceContract.CreateNewRaceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
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

    [HttpPatch("{raceId:guid}")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> PatchRace(Guid raceId, [FromBody] RaceContract.PatchRaceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var command = _mapper.Map<PatchRaceCommand>(request);
            command.RaceId = raceId;

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponseModel<RaceDetailResultModel>(
                APIContansts.StatusCode.Success,
                APIContansts.StatusMessage.Success,
                data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing PatchRace: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
