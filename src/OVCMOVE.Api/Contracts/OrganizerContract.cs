namespace OVCMOVE.Api.Contracts;

public static class OrganizerContract
{
    public sealed class CreateOrganizerRequest
    {
        public string Email { get; set; } = string.Empty;
        public List<Guid> RoleIds { get; set; } = [];
    }

    public sealed class OrganizerResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public sealed class OrganizerStatusResponse
    {
        public Guid OrganizerId { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    public sealed class OrganizerListItemResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
        public string Role { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    public sealed class OrganizerSearchItemResponse
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? AvatarUrl { get; init; }
    }

    public sealed class UpdateOrganizerRequest
    {
        public string DisplayName { get; init; } = string.Empty;
        public List<Guid> RoleIds { get; init; } = [];
        public string Status { get; init; } = string.Empty;
    }
}
