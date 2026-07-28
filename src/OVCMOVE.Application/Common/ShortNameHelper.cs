using System.Text;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Common;

public static class ShortNameHelper
{
    public static async Task<string> GenerateUniqueAsync(string email, IUserRepository userRepository, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var baseShortName = BuildBaseShortName(email);
        var candidate = baseShortName;
        var suffix = 1;

        while (await userRepository.GetByShortNameAsync(candidate, cancellationToken) is not null)
        {
            candidate = $"{baseShortName}{suffix}";
            suffix++;
        }

        return candidate;
    }

    public static string BuildBaseShortName(string email)
    {
        var localPart = email.Split('@', 2, StringSplitOptions.TrimEntries)[0];
        var builder = new StringBuilder(localPart.Length);

        foreach (var character in localPart.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.Length == 0 ? "user" : builder.ToString();
    }
}
