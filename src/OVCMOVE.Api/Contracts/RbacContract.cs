namespace OVCMOVE.Api.Contracts;

public static class RbacContract
{
    public class UpsertRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpsertPermissionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public sealed class RoleResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsSystem { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime ModifiedAt { get; init; }
        public int PermissionCount { get; init; }
        public IReadOnlyCollection<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
    }

    public sealed class PermissionResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Module { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public bool IsSystem { get; init; }
    }

    public sealed class UserRoleAssignmentResponse
    {
        public Guid UserId { get; init; }
        public Guid RoleId { get; init; }
    }

    public sealed class RolePermissionAssignmentResponse
    {
        public Guid RoleId { get; init; }
        public Guid PermissionId { get; init; }
    }
}
