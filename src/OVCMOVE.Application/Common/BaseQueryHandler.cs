using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace OVCMOVE.Application.Common
{
    public class BaseQueryHandler<T>
    {
        public readonly ILogger<T> _logger;

        public BaseQueryHandler(ILogger<T> logger)
        {
            _logger = logger;
        }
    }
}
