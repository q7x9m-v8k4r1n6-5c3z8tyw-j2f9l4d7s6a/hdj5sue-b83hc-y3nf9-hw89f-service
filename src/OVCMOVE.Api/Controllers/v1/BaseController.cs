using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace OVCMOVE.Api.Controllers.v1;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[EnableRateLimiting("InternalApiPolicy")]
public abstract class BaseController : ControllerBase
{
    protected readonly IMediator _mediator;

    protected BaseController(IMediator mediator)
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
