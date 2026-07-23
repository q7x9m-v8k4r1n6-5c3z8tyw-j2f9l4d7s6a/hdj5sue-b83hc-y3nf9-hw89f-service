using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Application.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (!passwordHash.StartsWith("$2", StringComparison.Ordinal))
        {
            return password == passwordHash;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
