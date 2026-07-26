namespace OVCMOVE.Application.DTOs.Security;

public class RoleAccessModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
}

public class PermissionAccessModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Module { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool IsSystem { get; init; }
}

public class UserAccessProfileModel
{
    public IReadOnlyCollection<RoleAccessModel> Roles { get; init; } = Array.Empty<RoleAccessModel>();
    public IReadOnlyCollection<PermissionAccessModel> Permissions { get; init; } = Array.Empty<PermissionAccessModel>();
    public IReadOnlyCollection<string> Access { get; init; } = Array.Empty<string>();
}
