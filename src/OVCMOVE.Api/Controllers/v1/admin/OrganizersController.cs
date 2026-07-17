using MediatR;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Controllers.v1;
using OVCMOVE.Application.DTOs.Organizer;
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
            var command = _mapper.Map<CreateOrganizerCommand>(request);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponseModel<OrganizerResponse>
            {
                StatusCode = APIContansts.StatusCode.Success,
                Message = APIContansts.StatusMessage.Success,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating organizer.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
