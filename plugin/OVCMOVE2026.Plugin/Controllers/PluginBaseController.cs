using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OVCMOVE2026.Plugin.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "plugin-2026")] 
public abstract class PluginBaseController : ControllerBase
{
    protected readonly IMediator _mediator;

    protected PluginBaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected Guid GetRequiredCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token không hợp lệ.");
    }
}