using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OVCMOVE.Application.DTOs.Organizer;
using OVCMOVE.Application.Organizers.Commands;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[ApiController]
[Route("api/v1/admin/organizers")] 
public class OrganizersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganizer([FromBody] CreateOrganizerRequest request)
    {
        try
        {
            var command = new CreateOrganizerCommand
            {
                Email = request.Email,
                FullName = request.FullName,
                Password = request.Password
            };

            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(CreateOrganizer), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message switch
            {
                "Email đã được đăng ký." => Conflict(new { message = ex.Message }),
                "Connection string 'DbConfig:SQLServer:ConnectionString' is not configured." => StatusCode(500, new { message = ex.Message }),
                "Email service configuration is not configured." => StatusCode(500, new { message = ex.Message }),
                "Email service credentials are not configured." => StatusCode(500, new { message = ex.Message }),
                _ => BadRequest(new { message = ex.Message })
            };
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
