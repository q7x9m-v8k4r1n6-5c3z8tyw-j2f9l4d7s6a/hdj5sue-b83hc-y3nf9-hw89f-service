namespace OVCMOVE.Application.DTOs.Security;

public class RoleSummaryModel
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

public class PermissionSummaryModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Module { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool IsSystem { get; init; }
}

public class UserRoleAssignmentModel
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
}

public class RolePermissionAssignmentModel
{
    public Guid RoleId { get; init; }
    public Guid PermissionId { get; init; }
}
