using System.Security.Cryptography;

namespace OVCMOVE.Application.Helpers;

public static class PasswordHelper
{
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%^&*()-_=+";
    private const string AllCharacters = Lowercase + Uppercase + Digits + Symbols;

    public static string Generate()
    {
        var password = new char[16];
        password[0] = GetRandomCharacter(Lowercase);
        password[1] = GetRandomCharacter(Uppercase);
        password[2] = GetRandomCharacter(Digits);
        password[3] = GetRandomCharacter(Symbols);

        for (var index = 4; index < password.Length; index++)
        {
            password[index] = GetRandomCharacter(AllCharacters);
        }

        Shuffle(password);
        return new string(password);
    }

    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool Verify(string password, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || !passwordHash.StartsWith("$2", StringComparison.Ordinal))
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

    private static char GetRandomCharacter(string characters)
    {
        return characters[RandomNumberGenerator.GetInt32(characters.Length)];
    }

    private static void Shuffle(Span<char> characters)
    {
        for (var index = characters.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
        }
    }
}
