using MediatR;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Application.Features.Auth.Command.RemoveBan;

public class RemoveBanCommandHandler : IRequestHandler<RemoveBanCommand, bool>
{
    private readonly ILoginRateLimitService _loginRateLimitService;

    public RemoveBanCommandHandler(ILoginRateLimitService loginRateLimitService)
    {
        _loginRateLimitService = loginRateLimitService;
    }

    public Task<bool> Handle(RemoveBanCommand request, CancellationToken cancellationToken)
    {
        _loginRateLimitService.RemoveBan(request.IpAddress, request.Username);
        
        return Task.FromResult(true);
    }
}