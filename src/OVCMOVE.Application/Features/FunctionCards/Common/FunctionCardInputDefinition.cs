using System.Text.Json;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Common;

internal static class FunctionCardInputDefinition
{
    public static IReadOnlySet<string> GetKeys(JsonElement inputs)
    {
        if (inputs.ValueKind != JsonValueKind.Array)
            throw new ApplicationValidationException("Inputs của thẻ phải là một mảng JSON.");

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in inputs.EnumerateArray())
        {
            if (input.ValueKind != JsonValueKind.Object)
                throw new ApplicationValidationException(
                    "Mỗi input của thẻ phải là một object JSON.");
            if (!input.TryGetProperty("key", out var keyElement) ||
                keyElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(keyElement.GetString()))
                throw new ApplicationValidationException(
                    "Mỗi input của thẻ phải có key.");

            var key = keyElement.GetString()!;
            if (!string.Equals(key, key.Trim(), StringComparison.Ordinal))
                throw new ApplicationValidationException(
                    "Input key không được chứa khoảng trắng ở đầu hoặc cuối.");
            if (key.Length > 100)
                throw new ApplicationValidationException(
                    "Input key không được vượt quá 100 ký tự.");
            if (!keys.Add(key))
                throw new ApplicationValidationException(
                    $"Input key '{key}' bị trùng trong cấu hình thẻ.");
        }

        return keys;
    }

    public static IReadOnlySet<string> GetKeys(string inputsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(inputsJson);
            return GetKeys(document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new ApplicationValidationException(
                $"Cấu hình input của thẻ không phải JSON hợp lệ: {exception.Message}");
        }
    }
}
