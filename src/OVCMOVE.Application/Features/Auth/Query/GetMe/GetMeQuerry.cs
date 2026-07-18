using MediatR;

namespace OVCMOVE.Application.Features.Auth.Queries.GetMe;

public record GetMeResult(
    Guid Id,
    string Email,
    string Role,
    string? DisplayName,
    string Status);

public record GetMeQuery(Guid UserId) : IRequest<GetMeResult>;