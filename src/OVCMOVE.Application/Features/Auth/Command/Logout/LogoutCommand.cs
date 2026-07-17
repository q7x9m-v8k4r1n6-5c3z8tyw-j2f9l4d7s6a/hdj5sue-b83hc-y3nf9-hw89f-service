using MediatR;

namespace OVCMOVE.Application.Features.Auth.Command.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<bool>; 