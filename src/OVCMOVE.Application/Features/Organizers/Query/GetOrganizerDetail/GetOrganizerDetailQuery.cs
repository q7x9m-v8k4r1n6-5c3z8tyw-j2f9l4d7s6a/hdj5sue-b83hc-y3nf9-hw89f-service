using MediatR;

namespace OVCMOVE.Application.Features.Organizers.Query.GetOrganizerDetail;

public sealed record GetOrganizerDetailQuery(Guid OrganizerId) : IRequest<GetOrganizerDetailResult?>;

public sealed class GetOrganizerDetailResult
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "organizer";
    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];
    public string Status { get; init; } = string.Empty;
}
