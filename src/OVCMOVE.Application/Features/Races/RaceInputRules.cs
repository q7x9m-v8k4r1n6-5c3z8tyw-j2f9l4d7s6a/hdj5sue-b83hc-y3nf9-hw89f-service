using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races;

internal static class RaceInputRules
{
    private const int ShortTextMaxLength = 255;
    private const int DescriptionMaxLength = 500;

    /// <summary>Validates the complete race state before persistence.</summary>
    internal static void ValidateRace(
        string? name,
        string? place,
        DateTime timeStart,
        DateTime timeEnd)
    {
        ValidateRequiredText(
            name,
            "Tên trận đấu",
            ShortTextMaxLength);
        ValidateRequiredText(
            place,
            "Địa điểm",
            ShortTextMaxLength);

        if (timeEnd <= timeStart)
        {
            throw new ApplicationValidationException(
                "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }
    }

    /// <summary>Validates one complete booth state before persistence.</summary>
    internal static void ValidateBooth(
        string? name,
        string? place,
        string? description)
    {
        ValidateRequiredText(name, "Tên booth", ShortTextMaxLength);
        ValidateRequiredText(place, "Địa điểm booth", ShortTextMaxLength);
        ValidateOptionalText(
            description,
            "Mô tả booth",
            DescriptionMaxLength);
    }

    private static void ValidateRequiredText(
        string? value,
        string fieldName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationValidationException(
                $"{fieldName} không được để trống.");
        }

        ValidateOptionalText(value, fieldName, maxLength);
    }

    private static void ValidateOptionalText(
        string? value,
        string fieldName,
        int maxLength)
    {
        if (value?.Trim().Length > maxLength)
        {
            throw new ApplicationValidationException(
                $"{fieldName} không được vượt quá {maxLength} ký tự.");
        }
    }
}
