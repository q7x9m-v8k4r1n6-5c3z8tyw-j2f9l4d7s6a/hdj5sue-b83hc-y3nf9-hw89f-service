using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Auth.Command.GoogleLogin;

public record GoogleLoginCommand(string IdToken) : IRequest<LoginResultModel>;
