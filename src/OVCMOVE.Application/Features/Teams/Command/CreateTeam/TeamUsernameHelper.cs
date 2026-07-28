using System.Globalization;
using System.Text;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Teams.Command.CreateTeam;

internal static class TeamUsernameHelper
{
    internal static async Task<string> GenerateUniqueAsync(
        string displayName,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var baseUsername = BuildBaseUsername(displayName);
        var candidate = baseUsername;
        var suffix = 2;

        while (await userRepository.GetByUsernameAnyStatusAsync(
                   candidate,
                   cancellationToken) is not null)
        {
            var suffixText = $"-{suffix++}";
            candidate = baseUsername[..Math.Min(
                baseUsername.Length,
                255 - suffixText.Length)] + suffixText;
        }

        return candidate;
    }

    internal static string BuildBaseUsername(string displayName)
    {
        var normalized = displayName
            .Trim()
            .ToLowerInvariant()
            .Replace('đ', 'd')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if ((character is >= 'a' and <= 'z') || char.IsDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var username = builder.ToString().Trim('-');
        return username.Length == 0
            ? "team"
            : username[..Math.Min(255, username.Length)];
    }
}
