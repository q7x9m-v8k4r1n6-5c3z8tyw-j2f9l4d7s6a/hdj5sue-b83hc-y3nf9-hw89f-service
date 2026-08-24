using System.Text.Json;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Common;

public static class FunctionCardValidator
{
    private static readonly HashSet<string> Categories =
    [
        FunctionCardConstants.Category.Attack,
        FunctionCardConstants.Category.Defense,
        FunctionCardConstants.Category.Effect
    ];

    public static void Validate(
        string cardKey,
        string name,
        string description,
        string category,
        string? backgroundUrl,
        JsonElement inputs)
    {
        if (string.IsNullOrWhiteSpace(cardKey) || cardKey.Trim().Length > 100)
            throw new ApplicationValidationException("CardKey phải có từ 1 đến 100 ký tự.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255)
            throw new ApplicationValidationException("Tên thẻ phải có từ 1 đến 255 ký tự.");
        if (description.Trim().Length > 1000)
            throw new ApplicationValidationException("Mô tả thẻ không được vượt quá 1000 ký tự.");
        if (!Categories.Contains(category.Trim().ToLowerInvariant()))
            throw new ApplicationValidationException("Loại thẻ chỉ có thể là attack, defense hoặc effect.");
        if (backgroundUrl?.Length > 2048)
            throw new ApplicationValidationException("URL hình nền không được vượt quá 2048 ký tự.");
        if (inputs.ValueKind != JsonValueKind.Array)
            throw new ApplicationValidationException("Inputs của thẻ phải là một mảng JSON.");
        if (inputs.GetRawText().Length > 100_000)
            throw new ApplicationValidationException("Cấu hình input của thẻ quá lớn.");
        FunctionCardInputDefinition.GetKeys(inputs);
    }
}
