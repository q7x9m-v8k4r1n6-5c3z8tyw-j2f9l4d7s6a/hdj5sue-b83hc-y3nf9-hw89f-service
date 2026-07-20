using MediatR;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Controllers.v1;
using OVCMOVE.Application.DTOs.Organizer;
using OVCMOVE.Application.Features.Organizer.Command.ChangeOrganizerStatus;
using OVCMOVE.Application.Organizers.Commands;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/organizers")]
public class OrganizersController : BaseController<OrganizersController>
{
    public OrganizersController(ILogger<OrganizersController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganizer([FromBody] CreateOrganizerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var command = _mapper.Map<CreateOrganizerCommand>(request);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponseModel<OrganizerResponse>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating organizer.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPatch("{organizerId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateOrganizerAccount(
        Guid organizerId,
        CancellationToken cancellationToken)
    {
        return await ChangeOrganizerStatusAsync(
            organizerId,
            OrganizerConstants.OrganizerStatus.InActive,
            "Organizer account has been deactivated successfully.",
            cancellationToken);
    }

    [HttpPatch("{organizerId:guid}/activate")]
    public async Task<IActionResult> ActivateOrganizerAccount(
        Guid organizerId,
        CancellationToken cancellationToken)
    {
        return await ChangeOrganizerStatusAsync(
            organizerId,
            OrganizerConstants.OrganizerStatus.Active,
            "Organizer account has been activated successfully.",
            cancellationToken);
    }

    private async Task<IActionResult> ChangeOrganizerStatusAsync(
        Guid organizerId,
        string status,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _mediator.Send(
                new ChangeOrganizerStatusCommand
                {
                    OrganizerId = organizerId,
                    Status = status
                },
                cancellationToken);

            if (result is null)
            {
                return Ok(new ApiResponseModel<object>
                {
                    StatusCode = APIContansts.StatusCode.NotFound,
                    Message = "Organizer account was not found."
                });
            }

            return Ok(new ApiResponseModel<OrganizerStatusResponse>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = successMessage,
                Data = result
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while changing organizer account status.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
