using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace OVCMOVE.Application.Common
{
    public class BaseCommandHandler<T>
    {
        public readonly ILogger<T> _logger;
        public readonly IMapper _mapper;
        public BaseCommandHandler(ILogger<T> logger, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
        }
    }
}
