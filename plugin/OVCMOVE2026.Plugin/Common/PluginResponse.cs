namespace OVCMOVE2026.Plugin.Common;

public class PluginResponse<T>
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DetailError { get; init; } = string.Empty;
    public T? Data { get; init; }

    public PluginResponse(int statusCode, string message, string detailError = "", T? data = default)
    {
        StatusCode = statusCode;
        Message = message;
        DetailError = detailError;
        Data = data;
    }
}

public static class PluginResponse
{
    public static PluginResponse<T> Success<T>(T data, string message = "Success") =>
        new(200, message, data: data);

    public static PluginResponse<object> Error(int statusCode, string message, string detailError = "") =>
        new(statusCode, message, detailError);
}