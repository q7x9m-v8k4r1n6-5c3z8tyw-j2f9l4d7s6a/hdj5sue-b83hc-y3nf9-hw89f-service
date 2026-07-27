using OVCMOVE.Domain.Common;

namespace OVCMOVE.Domain.Entities;

/// <summary>
/// Chứa các thông tin của user
/// </summary>
public class User : BaseEntity
{
    // Optional credentials are used by team accounts for direct login.
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }

    // Email identity linked to this user (for Google login/contact).
    public string LinkedEmail { get; set; } = string.Empty;

    // Business classification only. Authorization is resolved through UserRoles.
    public string UserType { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
    public string? ShortName { get; set; }
    public string Status { get; set; } = string.Empty;
}
