using MediatR;
using OVCMOVE.Application.Features.Auth.Command.Login; 

namespace OVCMOVE.Application.Features.Auth.Command.GoogleLogin;

public record GoogleLoginCommand(string IdToken) : IRequest<LoginResponse>;