using System.Text.Json;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.FunctionCards.Common;

internal static class FunctionCardInputDefinition
{
    public static IReadOnlySet<string> GetKeys(string inputsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(inputsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new ApplicationValidationException("Inputs của thẻ phải là một mảng JSON.");

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var input in document.RootElement.EnumerateArray())
            {
                if (input.ValueKind != JsonValueKind.Object ||
                    !input.TryGetProperty("key", out var keyElement) ||
                    keyElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(keyElement.GetString()))
                    continue;

                var key = keyElement.GetString()!.Trim();
                if (!keys.Add(key))
                    throw new ApplicationValidationException(
                        $"Input key '{key}' bị trùng trong cấu hình thẻ.");
            }

            return keys;
        }
        catch (JsonException exception)
        {
            throw new ApplicationValidationException(
                $"Cấu hình input của thẻ không phải JSON hợp lệ: {exception.Message}");
        }
    }
}
