using MediatR;

namespace OVCMOVE.Application.Features.Auth.Command.Login;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId);

public record LoginCommand(
    string Username,
    string Password) : IRequest<LoginResponse>;