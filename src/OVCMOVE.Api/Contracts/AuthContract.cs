namespace OVCMOVE.Api.Contracts;

public class AuthContract
{
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

    public class LogoutRequest {}
    public class RefreshTokenRequest { }

    // --- RESPONSE ---
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiration { get; set; } 
        public Guid UserId { get; set; }
    }

    public class MeResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}