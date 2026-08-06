namespace OVCMOVE.Api.Common;

/// <summary>
/// Defines the HTTP status values and default messages used by the API envelope.
/// </summary>
public static class ApiStatus
{
    public static class Codes
    {
        public const int Success = StatusCodes.Status200OK;
        public const int BadRequest = StatusCodes.Status400BadRequest;
        public const int Unauthorized = StatusCodes.Status401Unauthorized;
        public const int Forbidden = StatusCodes.Status403Forbidden;
        public const int NotFound = StatusCodes.Status404NotFound;
        public const int Conflict = StatusCodes.Status409Conflict;
        public const int InternalServerError = StatusCodes.Status500InternalServerError;
        public const int TooManyRequests = StatusCodes.Status429TooManyRequests;
    }

    public static class Messages
    {
        public const string Success = "Success";
        public const string BadRequest = "Bad Request";
        public const string Unauthorized = "Unauthorized";
        public const string Forbidden = "Forbidden";
        public const string NotFound = "Not Found";
        public const string Conflict = "Conflict";
        public const string InternalServerError = "Internal Server Error";
        public const string TooManyRequests = "Too Many Requests";
    }
}
