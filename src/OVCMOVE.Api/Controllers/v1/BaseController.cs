using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace OVCMOVE.Api.Controllers.v1;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
public abstract class BaseController : ControllerBase
{
    protected readonly IMediator _mediator;

    protected BaseController(IMediator mediator)
    {
        _mediator = mediator;
    }
}
