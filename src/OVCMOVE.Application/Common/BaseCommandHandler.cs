using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;

namespace OVCMOVE.Application.Common
{
    public class BaseCommandHandler<T>
    {
        protected readonly ILogger<T> _logger;
        protected readonly IMapper _mapper;
        protected readonly IUnitOfWork? _unitOfWork;

        public BaseCommandHandler(ILogger<T> logger, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
        }

        public BaseCommandHandler(ILogger<T> logger, IMapper mapper, IUnitOfWork unitOfWork)
            : this(logger, mapper)
        {
            _unitOfWork = unitOfWork;
        }
    }
}
