using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
}
