using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace OVCMOVE.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[ApiExplorerSettings(GroupName = "v1")]
public abstract class BaseController<T> : ControllerBase
{
    protected readonly ILogger<T> _logger;
    protected readonly IMediator _mediator;
    protected readonly IMapper _mapper;

    public BaseController(ILogger<T> logger, IMediator mediator, IMapper mapper)
    {
        _logger = logger;
        _mediator = mediator;
        _mapper = mapper;
    }
}