using AutoMapper;
using MediatR;

namespace OVCMOVE.Api.Controllers.Plugin.Move2026
{
    public class PluginExampleController : BaseController<PluginExampleController>
    {
        public PluginExampleController(ILogger<PluginExampleController> logger, IMediator mediator, IMapper mapper) : base(logger, mediator, mapper)
        {
        }
    }
}