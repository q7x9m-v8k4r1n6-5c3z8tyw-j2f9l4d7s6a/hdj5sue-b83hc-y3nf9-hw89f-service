using System.Text.Json;

namespace OVCMOVE.Api.Common;

/// <summary>Deserializes JSON embedded in multipart form payloads.</summary>
public static class JsonPayloadDeserializer
{
    private static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web);

    public static T Deserialize<T>(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload không được để trống.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, Options)
                ?? throw new ArgumentException("Payload không hợp lệ.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Payload JSON không hợp lệ.",
                exception);
        }
    }
}
