using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Infrastructure.Services;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>Checks a plaintext password against a BCrypt hash.</summary>
    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
