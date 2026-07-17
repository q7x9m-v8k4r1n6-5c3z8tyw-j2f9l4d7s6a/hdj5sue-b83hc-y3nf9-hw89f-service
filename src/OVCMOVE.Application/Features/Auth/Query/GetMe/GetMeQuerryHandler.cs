using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Auth.Queries.GetMe;

public class GetMeQueryHandler : BaseQueryHandler<GetMeQueryHandler>, IRequestHandler<GetMeQuery, GetMeResult>
{
    private readonly IUserRepository _userRepository;

    public GetMeQueryHandler(
        IUserRepository userRepository, 
        ILogger<GetMeQueryHandler> logger) : base(logger)
    {
        _userRepository = userRepository;
    }

    public async Task<GetMeResult> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
            throw new UnauthorizedAccessException("Tài khoản không tồn tại");

        return new GetMeResult(
            user.Id,
            user.Email,
            user.Role,
            user.DisplayName,
            user.Status
        );
    }
}