using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Auth.Command.Refresh;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResultModel>;
