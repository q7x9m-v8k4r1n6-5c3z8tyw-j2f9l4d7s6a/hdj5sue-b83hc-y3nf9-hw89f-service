using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Example.Query.GetAllExampleByFilter;
using OVCMOVE.Domain.Constants;
using static OVCMOVE.Api.Contracts.ExampleContract;

namespace OVCMOVE.Api.Controllers.v1;

public class ExampleController : BaseController<ExampleController>
{
    public ExampleController(ILogger<ExampleController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAllExamplesByFilter(ExmampleRequestModel requestModel, CancellationToken cancellationToken)
    {
        try
        {
            var query = _mapper.Map<GetAllExampleByFilterQuery>(requestModel);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponseModel<ExampleResultModel.GetAllExamplesByFilterResultModel>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing GetAllExamplesByFilter: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
