namespace OVCMOVE.Api.Contracts;

public class AuthContract
{
    public class RoleAccessResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
    }

    public class PermissionAccessResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }

    // --- REQUEST ---
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class LogoutRequest { }
    public class RefreshTokenRequest { }

    public class RemoveBanRequest
    {
        public string IpAddress { get; init;} = string.Empty;
        public string Username { get; init;} = string.Empty;
    }

    // --- RESPONSE ---
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; }
        public Guid UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public IReadOnlyCollection<RoleAccessResponse> Roles { get; set; } = Array.Empty<RoleAccessResponse>();
        public IReadOnlyCollection<PermissionAccessResponse> Permissions { get; set; } = Array.Empty<PermissionAccessResponse>();
        public IReadOnlyCollection<string> Access { get; set; } = Array.Empty<string>();
    }

    public class MeResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public IReadOnlyCollection<RoleAccessResponse> Roles { get; set; } = Array.Empty<RoleAccessResponse>();
        public IReadOnlyCollection<PermissionAccessResponse> Permissions { get; set; } = Array.Empty<PermissionAccessResponse>();
        public IReadOnlyCollection<string> Access { get; set; } = Array.Empty<string>();
        public string? DisplayName { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
