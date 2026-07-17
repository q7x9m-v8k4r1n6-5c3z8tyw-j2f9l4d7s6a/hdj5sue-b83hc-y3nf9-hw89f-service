using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

public class OrganizerController : BaseController<OrganizerController>
{
    public OrganizerController(ILogger<OrganizerController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpPatch("{organizerId:int}/deactivate")]
    public async Task<IActionResult> DeactivateOrganizerAccount(
        int organizerId,
        CancellationToken cancellationToken)
    {
        return await ChangeOrganizerStatus(organizerId, isActive: false, cancellationToken);
    }

    [HttpPatch("{organizerId:int}/activate")]
    public async Task<IActionResult> ActivateOrganizerAccount(
        int organizerId,
        CancellationToken cancellationToken)
    {
        return await ChangeOrganizerStatus(organizerId, isActive: true, cancellationToken);
    }

    private async Task<IActionResult> ChangeOrganizerStatus(
        int organizerId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(
                new ChangeOrganizerStatusCommand
                {
                    OrganizerId = organizerId,
                    IsActive = isActive
                },
                cancellationToken);

            return Ok(new ApiResponseModel<OrganizerResultModel.ChangeOrganizerStatusResultModel>
            {
                StatusCode = GetStatusCode(result),
                Message = result.Message,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occurred while processing ChangeOrganizerStatus: {Message}", ex.Message);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    private static int GetStatusCode(OrganizerResultModel.ChangeOrganizerStatusResultModel result)
    {
        if (result.IsSuccess)
        {
            return APIContansts.StatusCode.Success;
        }

        return result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? APIContansts.StatusCode.NotFound
            : APIContansts.StatusCode.BadRequest;
    }
}
