using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OVCMOVE.Application.Common
{
    public class BaseCommandHandler<T>
    {
        protected readonly ILogger<T> _logger;

        public BaseCommandHandler(ILogger<T> logger)
        {
            _logger = logger;
        }
    }
}
