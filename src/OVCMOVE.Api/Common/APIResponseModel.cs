namespace OVCMOVE.Api.Common;

public class ApiResponseModel<T>
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DetailError { get; init; } = string.Empty;
    public T? Data { get; init; }

    public ApiResponseModel()
    {
    }

    public ApiResponseModel(
        int statusCode,
        string message,
        string detailError = "",
        T? data = default)
    {
        StatusCode = statusCode;
        Message = message;
        DetailError = detailError;
        Data = data;
    }
}

public static class ApiResponse
{
    /// <summary>Creates a successful API response envelope.</summary>
    public static ApiResponseModel<T> Success<T>(
        T data,
        string message = ApiStatus.Messages.Success) =>
        new(ApiStatus.Codes.Success, message, data: data);

    /// <summary>Creates an error API response envelope.</summary>
    public static ApiResponseModel<object> Error(
        int statusCode,
        string message,
        string detailError = "") =>
        new(statusCode, message, detailError);
}
