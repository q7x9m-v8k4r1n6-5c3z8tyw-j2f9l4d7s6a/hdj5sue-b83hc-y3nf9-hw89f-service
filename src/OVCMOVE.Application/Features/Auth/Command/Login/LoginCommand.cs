using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Auth.Command.Login;

public record LoginResult( 
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiration,
    Guid UserId);

public record LoginCommand(
    string Username,
    string Password) : IRequest<LoginResultModel>; 