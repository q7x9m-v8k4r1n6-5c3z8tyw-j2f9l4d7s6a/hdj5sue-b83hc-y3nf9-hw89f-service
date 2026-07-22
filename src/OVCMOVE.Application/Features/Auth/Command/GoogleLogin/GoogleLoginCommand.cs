using MediatR;
using OVCMOVE.Application.Features.Auth.Command.Login; 
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Auth.Command.GoogleLogin;

public record GoogleLoginCommand(string IdToken) : IRequest<LoginResultModel>;