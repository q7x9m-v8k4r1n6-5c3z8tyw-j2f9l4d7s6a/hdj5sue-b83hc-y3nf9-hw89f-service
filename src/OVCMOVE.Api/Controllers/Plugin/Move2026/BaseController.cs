using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace OVCMOVE.Api.Controllers.Plugin.Move2026
{

    [ApiController]
    [Route("api/plugin/move2026/[controller]")]
    [ApiExplorerSettings(GroupName = "plugin-2026")]
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
}
