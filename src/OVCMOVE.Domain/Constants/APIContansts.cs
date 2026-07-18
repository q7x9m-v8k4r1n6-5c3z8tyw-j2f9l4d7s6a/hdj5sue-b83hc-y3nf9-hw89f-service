
namespace OVCMOVE.Domain.Constants
{
    public static class APIContansts
    {
        public static class StatusCode
        {
            public const int Success = 200;
            public const int BadRequest = 400;
            public const int Unauthorized = 401;
            public const int Forbidden = 403;
            public const int NotFound = 404;
            public const int InternalServerError = 500;
        }

        public static class StatusMessage
        {
            public const string Success = "Success";
            public const string BadRequest = "Bad Request";
            public const string Unauthorized = "Unauthorized";
            public const string Forbidden = "Forbidden";
            public const string NotFound = "Not Found";
            public const string InternalServerError = "Internal Server Error";
        }
    }
}
