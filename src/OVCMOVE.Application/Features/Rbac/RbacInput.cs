using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac;

internal static class RbacInput
{
    /// <summary>Trims a required RBAC field and returns a stable validation error.</summary>
    internal static string Required(
        string? value,
        string fieldName,
        int maxLength = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationValidationException(
                $"{fieldName} không được để trống.");
        }

        var normalized = value.Trim();
        EnsureMaxLength(normalized, fieldName, maxLength);
        return normalized;
    }

    /// <summary>Normalizes a required RBAC code for case-insensitive lookup.</summary>
    internal static string Code(
        string? value,
        string fieldName,
        int maxLength) =>
        Required(value, fieldName, maxLength).ToLowerInvariant();

    /// <summary>Trims and bounds an optional RBAC field.</summary>
    internal static string? Optional(
        string? value,
        string fieldName,
        int maxLength)
    {
        var normalized = value?.Trim();
        if (normalized is not null)
        {
            EnsureMaxLength(normalized, fieldName, maxLength);
        }

        return normalized;
    }

    private static void EnsureMaxLength(
        string value,
        string fieldName,
        int maxLength)
    {
        if (value.Length > maxLength)
        {
            throw new ApplicationValidationException(
                $"{fieldName} không được vượt quá {maxLength} ký tự.");
        }
    }
}
