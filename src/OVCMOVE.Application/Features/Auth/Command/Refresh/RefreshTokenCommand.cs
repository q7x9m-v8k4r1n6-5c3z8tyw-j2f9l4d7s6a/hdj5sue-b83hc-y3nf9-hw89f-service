using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Auth.Command.Login; 

namespace OVCMOVE.Application.Features.Auth.Command.Refresh;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultModel>;