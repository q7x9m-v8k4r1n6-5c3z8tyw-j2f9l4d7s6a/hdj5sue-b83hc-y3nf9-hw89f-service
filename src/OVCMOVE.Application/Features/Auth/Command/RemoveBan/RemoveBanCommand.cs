using MediatR;

namespace OVCMOVE.Application.Features.Auth.Command.RemoveBan;

public record RemoveBanCommand(string? IpAddress, string? Username) : IRequest<bool>;